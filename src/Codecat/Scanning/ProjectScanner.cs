using System.Security.Cryptography;
using System.Text;
using Codecat.Plugins;

namespace Codecat.Scanning;

internal sealed class ProjectScanner(IReadOnlyList<ICodecatPlugin> plugins, string outputPath)
{
    private const long MaxFileBytes = 1_000_000;
    private readonly string _outputPath = Path.GetFullPath(outputPath);

    public List<CodecatFile> Scan(string root)
    {
        var results = new List<CodecatFile>();
        var rootFullPath = Path.GetFullPath(root);

        Walk(rootFullPath, rootFullPath, results);
        results.Sort(static (left, right) => string.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private void Walk(string root, string currentDirectory, List<CodecatFile> results)
    {
        foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
        {
            var relative = ToRelativePath(root, directory);
            if (plugins.Any(plugin => plugin.ShouldIgnoreDirectory(relative)))
            {
                continue;
            }

            Walk(root, directory, results);
        }

        foreach (var file in Directory.EnumerateFiles(currentDirectory))
        {
            var fullPath = Path.GetFullPath(file);
            if (string.Equals(fullPath, _outputPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsCodecatOutput(fullPath))
            {
                continue;
            }

            var relative = ToRelativePath(root, fullPath);
            var plugin = plugins.FirstOrDefault(candidate => candidate.ShouldIncludeFile(relative));
            if (plugin is null)
            {
                continue;
            }

            var info = new FileInfo(fullPath);
            if (info.Length > MaxFileBytes || LooksBinary(fullPath))
            {
                continue;
            }

            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            results.Add(new CodecatFile(
                RelativePath: relative,
                Plugin: plugin.Name,
                Language: plugin.TryGetLanguage(relative) ?? "text",
                Bytes: info.Length,
                Lines: CountLines(content),
                Sha256: ComputeSha256(fullPath),
                Content: content));
        }
    }

    private static string ToRelativePath(string root, string fullPath)
    {
        return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
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
