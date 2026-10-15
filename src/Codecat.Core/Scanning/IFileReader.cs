namespace Codecat.Scanning;

public sealed record FileReadResult(string Path, long OriginalBytes, string Sha256, string Content);

public interface IFileReader
{
    Task<Result<FileReadResult, ScanError>> ReadAsync(string fullPath, string relativePath, long maxFileBytes, CancellationToken cancellationToken);
}
