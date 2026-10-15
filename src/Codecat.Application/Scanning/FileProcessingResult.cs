namespace Codecat.Scanning;

public sealed record FileProcessingResult(
    CodecatFile? File,
    FileLineCount? LineCount,
    string? SkipReason,
    ScanError? Error)
{
    public static FileProcessingResult Included(CodecatFile file) => new(file, null, null, null);
    public static FileProcessingResult Counted(FileLineCount count) => new(null, count, null, null);
    public static FileProcessingResult Skipped(string reason) => new(null, null, reason, null);
    public static FileProcessingResult Failed(ScanError error) => new(null, null, null, error);
}
