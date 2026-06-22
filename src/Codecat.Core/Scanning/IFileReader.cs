namespace Codecat.Scanning;

public sealed record FileReadResult(string Path, long OriginalBytes, string Content);

public interface IFileReader
{
    Task<Result<FileReadResult, ScanError>> ReadAsync(string fullPath, string relativePath, CancellationToken cancellationToken);
}
