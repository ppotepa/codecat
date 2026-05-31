using System.Text;

namespace Codecat.Scanning;

internal static class GlobalScanRules
{
    private static readonly HashSet<string> AllowedHiddenDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".github"
    };

    private static readonly HashSet<string> IgnoredFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ai", ".avif", ".bmp", ".dll", ".dmg", ".doc", ".docx", ".eot", ".exe", ".gif",
        ".ico", ".jar", ".jpeg", ".jpg", ".map", ".mp3", ".mp4", ".msi", ".otf", ".pdf",
        ".png", ".pdb", ".ppt", ".pptx", ".so", ".sqlite", ".svg", ".tar", ".ttf", ".webm",
        ".webp", ".woff", ".woff2", ".xls", ".xlsx", ".zip"
    };

    public static string? TryDenyDirectory(string relativePath)
    {
        var directoryName = Path.GetFileName(relativePath);
        return directoryName.StartsWith('.') && !AllowedHiddenDirectories.Contains(directoryName)
            ? "hidden_directory"
            : null;
    }

    public static string? TryDenyFileBeforePluginMatch(string fullPath, string relativePath, string outputPath)
    {
        if (IgnoredFileExtensions.Contains(Path.GetExtension(relativePath)))
        {
            return "ignored_extension";
        }

        if (string.Equals(Path.GetFullPath(fullPath), outputPath, StringComparison.OrdinalIgnoreCase))
        {
            return "output_file";
        }

        return null;
    }

    public static string? TryDenyFileAfterPluginMatch(string fullPath, FileInfo info, long maxFileBytes)
    {
        if (info.Length > maxFileBytes)
        {
            return "too_large";
        }

        if (LooksBinary(fullPath))
        {
            return "binary";
        }

        if (IsCodecatOutput(fullPath))
        {
            return "codecat_output";
        }

        return null;
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
}
