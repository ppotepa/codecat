using System.Security.Cryptography;
using System.Text;
using Codecat.Minification;
using Codecat.Plugins;

namespace Codecat.Scanning;

internal sealed class ProjectScanner(
    IReadOnlyList<ICodecatPlugin> plugins,
    string outputPath,
    ScanOptions options,
    Action<ScanProgress>? progress = null)
{
    private readonly string _outputPath = Path.GetFullPath(outputPath);
    private readonly MinifierRegistry _minifiers = MinifierRegistry.CreateDefault();
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
        var gitignoreRules = options.UseGitignore
            ? LoadGitignoreRules(rootFullPath, rootFullPath, GitignoreRules.Empty)
            : GitignoreRules.Empty;

        Walk(rootFullPath, rootFullPath, gitignoreRules);
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

    private void Walk(string root, string currentDirectory, GitignoreRules gitignoreRules)
    {
        _directoriesVisited++;
        ReportProgress(ToRelativePath(root, currentDirectory));

        foreach (var directory in SafeEnumerateDirectories(root, currentDirectory))
        {
            var relative = ToRelativePath(root, directory);
            var globalDenyReason = GlobalScanRules.TryDenyDirectory(relative);
            if (globalDenyReason is not null)
            {
                Skip(globalDenyReason);
                ReportVerbose($"skip dir: {relative} ({globalDenyReason})");
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: true))
            {
                Skip("gitignore");
                ReportVerbose($"skip dir: {relative} (gitignore)");
                continue;
            }

            if (plugins.Any(plugin => plugin.ShouldIgnoreDirectory(relative)))
            {
                Skip("ignored_directory");
                ReportVerbose($"skip dir: {relative}");
                continue;
            }

            var childGitignoreRules = options.UseGitignore
                ? LoadGitignoreRules(root, directory, gitignoreRules)
                : gitignoreRules;
            Walk(root, directory, childGitignoreRules);
        }

        foreach (var file in SafeEnumerateFiles(root, currentDirectory))
        {
            _filesSeen++;

            var fullPath = Path.GetFullPath(file);
            var relative = ToRelativePath(root, fullPath);

            var globalDenyReason = GlobalScanRules.TryDenyFileBeforePluginMatch(fullPath, relative, _outputPath);
            if (globalDenyReason is not null)
            {
                Skip(globalDenyReason);
                ReportVerbose($"skip file: {relative} ({globalDenyReason})");
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: false))
            {
                Skip("gitignore");
                ReportVerbose($"skip file: {relative} (gitignore)");
                continue;
            }

            if (options.ExtensionFilter is not null && options.ExtensionFilter.Count > 0)
            {
                var extension = Path.GetExtension(relative);
                if (!options.ExtensionFilter.Contains(extension))
                {
                    Skip("extension_filter");
                    ReportVerbose($"skip file: {relative} (extension_filter)");
                    continue;
                }
            }

            var pluginMatch = plugins
                .Select(plugin => plugin.TryMatchFile(relative))
                .FirstOrDefault(match => match is not null);
            if (pluginMatch is null)
            {
                Skip("no_plugin_match");
                ReportVerbose($"skip file: {relative} (no plugin match)");
                continue;
            }

            if (!TryReadFile(fullPath, relative, pluginMatch))
            {
                continue;
            }

            ReportProgress(relative);
        }
    }

    private bool TryReadFile(string fullPath, string relative, PluginMatch pluginMatch)
    {
        try
        {
            var info = new FileInfo(fullPath);
            var safetyDenyReason = GlobalScanRules.TryDenyFileAfterPluginMatch(fullPath, info, options.MaxFileBytes);
            if (safetyDenyReason is not null)
            {
                Skip(safetyDenyReason);
                ReportVerbose($"skip file: {relative} ({safetyDenyReason})");
                return false;
            }

            var originalContent = File.ReadAllText(fullPath, Encoding.UTF8);
            var content = originalContent;
            var minified = false;
            if (options.Mini && _minifiers.TryMinify(originalContent, pluginMatch.Language, out var minifiedContent))
            {
                content = minifiedContent;
                minified = true;
            }

            _files.Add(new CodecatFile(
                RelativePath: relative,
                Plugin: pluginMatch.PluginName,
                Language: pluginMatch.Language,
                Reason: pluginMatch.Reason,
                Bytes: Encoding.UTF8.GetByteCount(content),
                Lines: CountLines(content),
                OriginalBytes: info.Length,
                OriginalLines: CountLines(originalContent),
                Minified: minified,
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

    private GitignoreRules LoadGitignoreRules(string root, string currentDirectory, GitignoreRules rules)
    {
        var gitignorePath = Path.Combine(currentDirectory, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return rules;
        }

        try
        {
            var baseRelativePath = ToRelativePath(root, currentDirectory);
            return rules.AddFromFile(gitignorePath, baseRelativePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Warn(ToRelativePath(root, gitignorePath), $"could not read .gitignore: {exception.Message}");
            return rules;
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
