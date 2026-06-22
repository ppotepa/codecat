namespace Codecat.Scanning;

public sealed record ScanOptions(
    long MaxFileBytes,
    bool Quiet,
    bool Verbose,
    bool Mini,
    bool UseGitignore,
    IReadOnlyCollection<string>? ExtensionFilter);
