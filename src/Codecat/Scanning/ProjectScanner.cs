using System.Security.Cryptography;
using System.Text;
using Codecat.Plugins;

namespace Codecat.Scanning;

internal sealed class ProjectScanner(
    IReadOnlyList<ICodecatPlugin> plugins,
    string outputPath,
    ScanOptions options,
    Action<ScanProgress>? progress = null)
{
    private static readonly HashSet<string> AllowedHiddenDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".github"
    };

    private static readonly HashSet<string> AlwaysIgnoredFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ai", ".avif", ".bmp", ".dll", ".dmg", ".doc", ".docx", ".eot", ".exe", ".gif",
        ".ico", ".jar", ".jpeg", ".jpg", ".map", ".mp3", ".mp4", ".msi", ".otf", ".pdf",
        ".png", ".pdb", ".ppt", ".pptx", ".so", ".sqlite", ".svg", ".tar", ".ttf", ".webm",
        ".webp", ".woff", ".woff2", ".xls", ".xlsx", ".zip"
    };

    private readonly string _outputPath = Path.GetFullPath(outputPath);
    private readonly List<CodecatFile> _files = [];
    private readonly List<ScanWarning> _warnings = [];
    private readonly Dictionary<string, int> _skippedByReason = new(StringComparer.OrdinalIgnoreCase);
    private int _directoriesVisited;
    private int _filesSeen;
    private int _filesIncluded;
    private int _itemsSkipped;

    public ScanResult Scan(string root)
    {
        var rootFullPath = Path.GetFullPath(root);

        Walk(rootFullPath, rootFullPath);
        _files.Sort(static (left, right) => string.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase));

        return new ScanResult(
            _files,
            _warnings,
            _skippedByReason,
            _directoriesVisited,
            _filesSeen,
            _filesIncluded,
            _itemsSkipped);
    }

    private void Walk(string root, string currentDirectory)
    {
        _directoriesVisited++;
        ReportProgress(ToRelativePath(root, currentDirectory));

        foreach (var directory in SafeEnumerateDirectories(root, currentDirectory))
        {
            var relative = ToRelativePath(root, directory);
            if (IsHiddenDirectory(relative))
            {
                Skip("hidden_directory");
                ReportVerbose($"skip dir: {relative} (hidden directory)");
                continue;
            }

            if (plugins.Any(plugin => plugin.ShouldIgnoreDirectory(relative)))
            {
                Skip("ignored_directory");
                ReportVerbose($"skip dir: {relative}");
                continue;
            }

            Walk(root, directory);
        }

        foreach (var file in SafeEnumerateFiles(root, currentDirectory))
        {
            _filesSeen++;

            var fullPath = Path.GetFullPath(file);
            var relative = ToRelativePath(root, fullPath);

            if (AlwaysIgnoredFileExtensions.Contains(Path.GetExtension(relative)))
            {
                Skip("ignored_extension");
                ReportVerbose($"skip file: {relative} (ignored extension)");
                continue;
            }

            if (string.Equals(fullPath, _outputPath, StringComparison.OrdinalIgnoreCase))
            {
                Skip("output_file");
                continue;
            }

            var plugin = plugins.FirstOrDefault(candidate => candidate.ShouldIncludeFile(relative));
            if (plugin is null)
            {
                Skip("no_plugin_match");
                ReportVerbose($"skip file: {relative} (no plugin match)");
                continue;
            }

            if (!TryReadFile(root, fullPath, relative, plugin))
            {
                continue;
            }

            ReportProgress(relative);
        }
    }

    private bool TryReadFile(string root, string fullPath, string relative, ICodecatPlugin plugin)
    {
        try
        {
            var info = new FileInfo(fullPath);
            if (info.Length > options.MaxFileBytes)
            {
                Skip("too_large");
                ReportVerbose($"skip file: {relative} (too large: {info.Length} bytes)");
                return false;
            }

            if (LooksBinary(fullPath))
            {
                Skip("binary");
                ReportVerbose($"skip file: {relative} (binary)");
                return false;
            }

            if (IsCodecatOutput(fullPath))
            {
                Skip("codecat_output");
                ReportVerbose($"skip file: {relative} (previous codecat output)");
                return false;
            }

            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            _files.Add(new CodecatFile(
                RelativePath: relative,
                Plugin: plugin.Name,
                Language: plugin.TryGetLanguage(relative) ?? "text",
                Bytes: info.Length,
                Lines: CountLines(content),
                Sha256: ComputeSha256(fullPath),
                Content: content));
            _filesIncluded++;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip("read_error");
            Warn(relative, $"could not read file: {exception.Message}");
            return false;
        }
    }

    private IEnumerable<string> SafeEnumerateDirectories(string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateDirectories(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip("directory_error");
            Warn(ToRelativePath(root, currentDirectory), $"could not enumerate directories: {exception.Message}");
            return [];
        }
    }

    private IEnumerable<string> SafeEnumerateFiles(string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip("directory_error");
            Warn(ToRelativePath(root, currentDirectory), $"could not enumerate files: {exception.Message}");
            return [];
        }
    }

    private void Skip(string reason)
    {
        _itemsSkipped++;
        _skippedByReason[reason] = _skippedByReason.GetValueOrDefault(reason) + 1;
    }

    private void Warn(string path, string message)
    {
        _warnings.Add(new ScanWarning(path, message));
        if (!options.Quiet)
        {
            Console.Error.WriteLine($"warning: {path}: {message}");
        }
    }

    private void ReportProgress(string currentPath)
    {
        if (options.Quiet || _filesSeen == 0 || _filesSeen % 100 != 0)
        {
            return;
        }

        progress?.Invoke(new ScanProgress(_directoriesVisited, _filesSeen, _filesIncluded, _itemsSkipped, currentPath));
    }

    private void ReportVerbose(string message)
    {
        if (options.Verbose && !options.Quiet)
        {
            Console.Error.WriteLine(message);
        }
    }

    private static string ToRelativePath(string root, string fullPath)
    {
        var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return relative == "." ? "." : relative;
    }

    private static bool IsHiddenDirectory(string relativePath)
    {
        var directoryName = Path.GetFileName(relativePath);
        return directoryName.StartsWith('.') && !AllowedHiddenDirectories.Contains(directoryName);
    }

    private static bool LooksBinary(string path)
    {
        Span<byte> buffer = stackalloc byte[512];
        using var stream = File.OpenRead(path);
        var read = stream.Read(buffer);
        return buffer[..read].Contains((byte)0);
    }

    private static bool IsCodecatOutput(string path)
    {
        Span<byte> buffer = stackalloc byte[16];
        using var stream = File.OpenRead(path);
        var read = stream.Read(buffer);
        return read >= 16 && Encoding.ASCII.GetString(buffer) == "CODECAT_VERSION:";
    }

    private static int CountLines(string content)
    {
        if (content.Length == 0)
        {
            return 0;
        }

        var newlines = 0;
        foreach (var character in content)
        {
            if (character == '\n')
            {
                newlines++;
            }
        }

        return content[^1] == '\n' ? newlines : newlines + 1;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
