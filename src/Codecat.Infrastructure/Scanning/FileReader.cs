using System.Security.Cryptography;
using System.Text;

namespace Codecat.Scanning;

public sealed class FileReader : IFileReader
{
    public async Task<Result<FileReadResult, ScanError>> ReadAsync(
        string fullPath,
        string relativePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                16_384,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
            var content = await reader.ReadToEndAsync(cancellationToken);
            var fileInfo = new FileInfo(fullPath);
            return Result<FileReadResult, ScanError>.Success(new FileReadResult(relativePath, fileInfo.Length, content));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            return Result<FileReadResult, ScanError>.Failure(new ScanError(ScanErrorKind.ReadFile, relativePath, exception.Message));
        }
    }

    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
