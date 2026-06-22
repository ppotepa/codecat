namespace Codecat.Scanning;

public enum ScanErrorKind
{
    ReadFile,
    DirectoryEnumeration,
    FileEnumeration,
    Gitignore,
    ComputeHash,
    MetricsCalculation
}

public sealed record ScanError(ScanErrorKind Kind, string Path, string Message);
