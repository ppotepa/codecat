using System.Text;
using Codecat.Scanning;

namespace Codecat.Output;

internal sealed class CodecatWriter
{
    public void Write(string root, string outputPath, IReadOnlyList<CodecatFile> files)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.WriteLine("CODECAT_VERSION: 1");
        writer.WriteLine($"ROOT: {root}");
        writer.WriteLine($"TOTAL_FILES: {files.Count}");
        writer.WriteLine($"TOTAL_LINES: {files.Sum(file => file.Lines)}");
        writer.WriteLine($"TOTAL_BYTES: {files.Sum(file => file.Bytes)}");
        writer.WriteLine($"PLUGIN_COUNTS: {FormatPluginCounts(files)}");
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
        writer.WriteLine("<<<END_SUMMARY>>>");
    }

    private static string FormatPluginCounts(IReadOnlyList<CodecatFile> files)
    {
        return string.Join(
            ';',
            files
                .GroupBy(file => file.Plugin)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => $"{group.Key}={group.Count()}"));
    }

    private static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
