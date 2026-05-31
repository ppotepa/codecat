namespace Codecat.Scanning;

internal sealed record ScanOptions(long MaxFileBytes, bool Quiet, bool Verbose);
