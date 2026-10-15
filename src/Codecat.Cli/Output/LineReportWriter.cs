using Codecat.Scanning;

namespace Codecat.Output;

public static class LineReportWriter
{
    public static void Write(TextWriter output, IReadOnlyList<FileLineCount> files, bool tree, int? depth)
    {
        output.WriteLine("EXTENSION  FILES  LINES");
        foreach (var group in files.GroupBy(file =>
            Path.GetExtension(file.RelativePath).ToLowerInvariant() is { Length: > 0 } extension
                ? extension : "[no extension]", StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            output.WriteLine($"{group.Key}  {group.Count()}  {group.Sum(file => file.Lines)}");
        }

        if (tree)
        {
            output.WriteLine("TREE  FILES  LINES");
            var folders = new Dictionary<string, (long Files, long Lines)>(PathRules.Comparer)
            {
                ["."] = (0, 0)
            };
            foreach (var file in files)
            {
                var parts = file.RelativePath.Split('/');
                Add(".", file.Lines);
                var folder = "";
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    folder = folder.Length == 0 ? parts[i] : $"{folder}/{parts[i]}";
                    Add(folder, file.Lines);
                }
            }

            foreach (var folder in folders.Keys.OrderBy(path => path == "." ? "" : path, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(path => path, StringComparer.Ordinal))
            {
                var level = folder == "." ? 0 : folder.Count(character => character == '/') + 1;
                if (depth is not null && level > depth) continue;
                var name = folder == "." ? "." : Path.GetFileName(folder);
                var totals = folders[folder];
                output.WriteLine($"{new string(' ', level * 2)}{name}{(folder == "." ? "" : "/")}  {totals.Files} files, {totals.Lines} lines");
            }

            void Add(string path, long lines)
            {
                var totals = folders.GetValueOrDefault(path);
                folders[path] = (totals.Files + 1, totals.Lines + lines);
            }
        }

        output.WriteLine($"TOTAL: {files.Count} files, {files.Sum(file => file.Lines)} lines");
    }
}
