using System.Security.Cryptography;
using System.Text;

namespace Codecat.Scanning;

public sealed class FileReader : IFileReader
{
    private const int BufferSize = 16_384;
    private const int SignatureLength = 16;

    public async Task<Result<FileReadResult, ScanError>> ReadAsync(
        string fullPath,
        string relativePath,
        long maxFileBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (maxFileBytes > 0 && stream.Length > maxFileBytes)
                return Failure(ScanErrorKind.TooLarge, relativePath, "file exceeds configured size limit");
            using var content = new MemoryStream(stream.Length is > 0 and <= int.MaxValue ? (int)stream.Length : 0);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[BufferSize];
            long totalBytes = 0;
            var read = await stream.ReadAsync(buffer, cancellationToken);
            while (read is > 0 and < SignatureLength)
            {
                var additional = await stream.ReadAsync(buffer.AsMemory(read, SignatureLength - read), cancellationToken);
                if (additional == 0) break;
                read += additional;
            }
            if (read > 0)
            {
                totalBytes = read;
                if (maxFileBytes > 0 && totalBytes > maxFileBytes)
                    return Failure(ScanErrorKind.TooLarge, relativePath, "file exceeds configured size limit");
                var prefix = buffer.AsSpan(0, read);
                if (LooksBinary(prefix)) return Failure(ScanErrorKind.Binary, relativePath, "file contains binary data");
                if (IsCodecatOutput(prefix)) return Failure(ScanErrorKind.PreviousOutput, relativePath, "file is a previous Codecat output");
                hash.AppendData(buffer, 0, read);
                content.Write(buffer, 0, read);
            }
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                totalBytes += read;
                if (maxFileBytes > 0 && totalBytes > maxFileBytes)
                    return Failure(ScanErrorKind.TooLarge, relativePath, "file exceeds configured size limit");
                if (!HasUnicodeByteOrderMark(content.GetBuffer().AsSpan(0, checked((int)content.Length))) && buffer.AsSpan(0, read).Contains((byte)0))
                    return Failure(ScanErrorKind.Binary, relativePath, "file contains binary data");
                hash.AppendData(buffer, 0, read);
                content.Write(buffer, 0, read);
            }

            var bytes = content.GetBuffer().AsSpan(0, checked((int)content.Length));

            cancellationToken.ThrowIfCancellationRequested();
            var encoding = DetectEncoding(bytes, out var preambleLength);
            var decoded = encoding.GetString(bytes[preambleLength..]);
            return Result<FileReadResult, ScanError>.Success(new FileReadResult(
                relativePath, totalBytes, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), decoded));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException or DecoderFallbackException)
        {
            return Failure(ScanErrorKind.ReadFile, relativePath, exception.Message);
        }
    }

    private static Result<FileReadResult, ScanError> Failure(ScanErrorKind kind, string path, string message) =>
        Result<FileReadResult, ScanError>.Failure(new ScanError(kind, path, message));

    private static bool LooksBinary(ReadOnlySpan<byte> bytes)
    {
        if (HasUnicodeByteOrderMark(bytes)) return false;
        return bytes.Contains((byte)0);
    }

    private static bool HasUnicodeByteOrderMark(ReadOnlySpan<byte> bytes) =>
        (bytes.Length >= 4 &&
         ((bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) ||
          (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF))) ||
        (bytes.Length >= 2 &&
         ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)));

    private static Encoding DetectEncoding(ReadOnlySpan<byte> bytes, out int preambleLength)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            preambleLength = 4;
            return new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);
        }
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            preambleLength = 4;
            return new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            preambleLength = 2;
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            preambleLength = 2;
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
        }
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            preambleLength = 3;
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        }

        preambleLength = 0;
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }

    private static bool IsCodecatOutput(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith("CODECAT_VERSION:"u8) || bytes.StartsWith("CC1|"u8);
}
