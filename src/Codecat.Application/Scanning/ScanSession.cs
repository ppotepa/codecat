namespace Codecat.Scanning;

internal sealed class ScanSession
{
    internal Dictionary<string, int> SkippedByReason { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal List<ScanWarning> Warnings { get; } = [];
    internal List<CodecatFile> Files { get; } = [];
    internal List<FileLineCount> LineCounts { get; } = [];
    internal int DirectoriesVisited { get; set; }
    internal int FilesSeen { get; set; }
    internal int FilesIncluded { get; set; }
    internal int ItemsSkipped { get; set; }

    internal ScanResult Complete()
    {
        Files.Sort((left, right) => ComparePaths(left.RelativePath, right.RelativePath));
        LineCounts.Sort((left, right) => ComparePaths(left.RelativePath, right.RelativePath));
        return new ScanResult(Files.ToArray(), Warnings.ToArray(),
            SkippedByReason.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            DirectoriesVisited, FilesSeen, FilesIncluded, ItemsSkipped, LineCounts.ToArray());
    }

    private static int ComparePaths(string left, string right)
    {
        var insensitive = PathRules.Comparer.Compare(left, right);
        return insensitive != 0 ? insensitive : StringComparer.Ordinal.Compare(left, right);
    }
}
