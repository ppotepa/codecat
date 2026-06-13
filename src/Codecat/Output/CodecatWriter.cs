using System.Text;
using System.IO;
using Codecat.Scanning;

namespace Codecat.Output;

internal sealed class CodecatWriter
{
    public void Write(string root, string outputPath, ScanResult result, bool mini)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        if (mini)
        {
            WriteMini(root, result, writer);
        }
        else
        {
            WriteDefault(root, result, writer);
        }
    }

    private static void WriteDefault(string root, ScanResult result, TextWriter writer)
    {
        var files = result.Files;
        writer.WriteLine("CODECAT_VERSION: 1");
        writer.WriteLine($"ROOT: {root}");
        writer.WriteLine($"TOTAL_FILES: {files.Count}");
        writer.WriteLine($"TOTAL_LINES: {files.Sum(file => file.Lines)}");
        writer.WriteLine($"TOTAL_BYTES: {files.Sum(file => file.Bytes)}");
        writer.WriteLine($"PLUGIN_COUNTS: {FormatPluginCounts(files)}");
        writer.WriteLine($"DIRECTORIES_VISITED: {result.DirectoriesVisited}");
        writer.WriteLine($"FILES_SEEN: {result.FilesSeen}");
        writer.WriteLine($"ITEMS_SKIPPED: {result.ItemsSkipped}");
        writer.WriteLine($"WARNINGS: {result.Warnings.Count}");
        writer.WriteLine($"SKIPPED_BY_REASON: {FormatCounts(result.SkippedByReason)}");
        writer.WriteLine();

        foreach (var file in files)
        {
            writer.WriteLine($"<<<FILE path=\"{EscapeAttribute(file.RelativePath)}\" plugin=\"{EscapeAttribute(file.Plugin)}\" lang=\"{EscapeAttribute(file.Language)}\" reason=\"{EscapeAttribute(file.Reason)}\" lines=\"{file.Lines}\" bytes=\"{file.Bytes}\" original_lines=\"{file.OriginalLines}\" original_bytes=\"{file.OriginalBytes}\" minified=\"{file.Minified.ToString().ToLowerInvariant()}\" sha256=\"{file.Sha256}\">>>");
            writer.Write(file.Content);
            if (file.Content.Length > 0 && file.Content[^1] != '\n')
            {
                writer.WriteLine();
            }

            writer.WriteLine("<<<END_FILE>>>");
            writer.WriteLine();
        }

        writer.WriteLine("<<<SUMMARY>>>");
        writer.WriteLine($"included_files={files.Count}");
        writer.WriteLine($"total_lines={files.Sum(file => file.Lines)}");
        writer.WriteLine($"total_bytes={files.Sum(file => file.Bytes)}");
        writer.WriteLine($"plugin_counts={FormatPluginCounts(files)}");
        writer.WriteLine($"directories_visited={result.DirectoriesVisited}");
        writer.WriteLine($"files_seen={result.FilesSeen}");
        writer.WriteLine($"items_skipped={result.ItemsSkipped}");
        writer.WriteLine($"warnings={result.Warnings.Count}");
        writer.WriteLine($"skipped_by_reason={FormatCounts(result.SkippedByReason)}");
        foreach (var warning in result.Warnings.Take(50))
        {
            writer.WriteLine($"warning path=\"{EscapeAttribute(warning.Path)}\" message=\"{EscapeAttribute(warning.Message)}\"");
        }

        writer.WriteLine("<<<END_SUMMARY>>>");
    }

    private static void WriteMini(string root, ScanResult result, TextWriter writer)
    {
        var files = result.Files;
        writer.WriteLine($"CC1|root={EscapeField(root)}|files={files.Count}|lines={files.Sum(file => file.Lines)}|bytes={files.Sum(file => file.Bytes)}|seen={result.FilesSeen}|skipped={result.ItemsSkipped}|warnings={result.Warnings.Count}");

        foreach (var file in files)
        {
            writer.WriteLine($"F|{EscapeField(file.RelativePath)}|{EscapeField(file.Plugin)}|{EscapeField(file.Language)}|{file.Lines}|{file.Bytes}|{file.OriginalLines}|{file.OriginalBytes}|{(file.Minified ? "m" : "-")}|{EscapeField(file.Reason)}");
            writer.Write(NormalizeLineEndings(file.Content));
            if (file.Content.Length > 0 && file.Content[^1] != '\n')
            {
                writer.WriteLine();
            }

            writer.WriteLine("E");
        }

        var skipped = FormatCounts(result.SkippedByReason);
        if (skipped.Length > 0)
        {
            writer.WriteLine($"S|{EscapeField(skipped)}");
        }

        foreach (var warning in result.Warnings.Take(50))
        {
            writer.WriteLine($"W|{EscapeField(warning.Path)}|{EscapeField(warning.Message)}");
        }
    }

    private static string FormatPluginCounts(IReadOnlyList<CodecatFile> files)
    {
        return FormatCounts(files
            .GroupBy(file => file.Plugin)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatCounts(IReadOnlyDictionary<string, int> counts)
    {
        return string.Join(
            ';',
            counts
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static string EscapeField(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }
}
