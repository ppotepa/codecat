using System.Text;
using Codecat.Scanning;

namespace Codecat.Output;

internal sealed class CodecatWriter
{
    public void Write(string root, string outputPath, ScanResult result)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var files = result.Files;
        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
            writer.WriteLine($"<<<FILE path=\"{EscapeAttribute(file.RelativePath)}\" plugin=\"{EscapeAttribute(file.Plugin)}\" lang=\"{EscapeAttribute(file.Language)}\" lines=\"{file.Lines}\" bytes=\"{file.Bytes}\" sha256=\"{file.Sha256}\">>>");
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
}
