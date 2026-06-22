namespace Codecat.Scanning;

public sealed record ScanResult(
    IReadOnlyList<CodecatFile> Files,
    IReadOnlyList<ScanWarning> Warnings,
    IReadOnlyDictionary<string, int> SkippedByReason,
    int DirectoriesVisited,
    int FilesSeen,
    int FilesIncluded,
    int ItemsSkipped);
