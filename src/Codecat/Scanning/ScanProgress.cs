namespace Codecat.Scanning;

internal sealed record ScanProgress(
    int DirectoriesVisited,
    int FilesSeen,
    int FilesIncluded,
    int ItemsSkipped,
    string CurrentPath);
