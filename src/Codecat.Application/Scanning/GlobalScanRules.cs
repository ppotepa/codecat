using System.Text;

namespace Codecat.Scanning;

public static class GlobalScanRules
{
    private static readonly HashSet<string> AllowedHiddenDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".github"
    };

    private static readonly HashSet<string> IgnoredFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".a", ".aab", ".aar", ".ai", ".apk", ".avif", ".bmp", ".class", ".db", ".dex",
        ".dll", ".dmg", ".doc", ".docx", ".eot", ".exe", ".gif", ".ico", ".jar", ".jpeg",
        ".jpg", ".map", ".mp3", ".mp4", ".msi", ".o", ".otf", ".pdf", ".png", ".pdb",
        ".ppt", ".pptx", ".so", ".sqlite", ".svg", ".tar", ".ttf", ".webm", ".webp",
        ".woff", ".woff2", ".xls", ".xlsx", ".zip"
    };

    public static string? TryDenyDirectory(string relativePath)
    {
        var directoryName = Path.GetFileName(relativePath);
        if (directoryName.StartsWith('.') && !AllowedHiddenDirectories.Contains(directoryName))
        {
            return "hidden_directory";
        }

        return GlobalDirectoryRules.IsIgnored(directoryName) ? "ignored_directory" : null;
    }

    public static string? TryDenyFileBeforePluginMatch(string fullPath, string relativePath, string? outputPath)
    {
        if (IgnoredFileExtensions.Contains(Path.GetExtension(relativePath)))
        {
            return "ignored_extension";
        }

        if (outputPath is not null && PathRules.Comparer.Equals(Path.GetFullPath(fullPath), outputPath))
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
        var hasUtf32Bom = read >= 4 &&
            ((buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00) ||
             (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF));
        var hasUtf16Bom = read >= 2 &&
            ((buffer[0] == 0xFF && buffer[1] == 0xFE) || (buffer[0] == 0xFE && buffer[1] == 0xFF));
        if (hasUtf32Bom || hasUtf16Bom) return false;
        return buffer[..read].Contains((byte)0);
    }

    private static bool IsCodecatOutput(string path)
    {
        Span<byte> buffer = stackalloc byte[16];
        using var stream = File.OpenRead(path);
        var read = stream.Read(buffer);
        if (read >= 16 && Encoding.ASCII.GetString(buffer) == "CODECAT_VERSION:")
        {
            return true;
        }

        return read >= 4 && Encoding.ASCII.GetString(buffer[..4]) == "CC1|";
    }
}
