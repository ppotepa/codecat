using Codecat.Minification;
using Codecat.Plugins;

namespace Codecat.Scanning;

public sealed class FileProcessor(
    IFileReader fileReader,
    IFileLineCounter lineCounter,
    IMinifierRegistry minifiers,
    IMetricsCalculator metricsCalculator)
{
    public async Task<FileProcessingResult> ProcessAsync(
        string fullPath,
        string relativePath,
        PluginMatch pluginMatch,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        if (options.LinesOnly)
        {
            try
            {
                var info = new FileInfo(fullPath);
                var reason = GlobalScanRules.TryDenyFileAfterPluginMatch(fullPath, info, options.MaxFileBytes);
                if (reason is not null) return FileProcessingResult.Skipped(reason);
                return FileProcessingResult.Counted(new FileLineCount(relativePath,
                    await lineCounter.CountAsync(fullPath, cancellationToken)));
            }
            catch (Exception exception) when (IsFileAccessException(exception))
            {
                return ReadFailure(relativePath, exception, ScanErrorKind.ReadFile);
            }
        }

        Result<FileReadResult, ScanError> read;
        try
        {
            read = await fileReader.ReadAsync(fullPath, relativePath, options.MaxFileBytes, cancellationToken);
        }
        catch (Exception exception) when (IsFileAccessException(exception))
        {
            return ReadFailure(relativePath, exception, ScanErrorKind.ReadFile);
        }

        if (!read.IsSuccess)
        {
            var error = read.Errors.IsDefaultOrEmpty
                ? new ScanError(ScanErrorKind.ReadFile, relativePath, "unknown read error")
                : read.Errors[0];
            return error.Kind switch
            {
                ScanErrorKind.TooLarge => FileProcessingResult.Skipped("too_large"),
                ScanErrorKind.Binary => FileProcessingResult.Skipped("binary"),
                ScanErrorKind.PreviousOutput => FileProcessingResult.Skipped("codecat_output"),
                _ => FileProcessingResult.Failed(error)
            };
        }

        try
        {
            var snapshot = read.Value!;
            var processed = options.Mini ? NormalizeLineEndings(snapshot.Content) : snapshot.Content;
            var minified = false;
            if (options.Mini && minifiers.TryMinify(snapshot.Content, pluginMatch.Language, out var minifiedContent))
            {
                processed = minifiedContent;
                minified = true;
            }

            var metrics = metricsCalculator.Calculate(processed, snapshot.Content, snapshot.OriginalBytes, snapshot.Sha256);
            return FileProcessingResult.Included(new CodecatFile(
                RelativePath: relativePath,
                Plugin: pluginMatch.PluginName,
                Language: pluginMatch.Language,
                Reason: pluginMatch.Reason,
                Bytes: metrics.Bytes,
                Lines: metrics.Lines,
                OriginalBytes: metrics.OriginalBytes,
                OriginalLines: metrics.OriginalLines,
                Minified: minified,
                Sha256: metrics.Sha256,
                Content: processed));
        }
        catch (Exception exception) when (IsFileAccessException(exception))
        {
            return ReadFailure(relativePath, exception, ScanErrorKind.MetricsCalculation);
        }
    }

    private static FileProcessingResult ReadFailure(string path, Exception exception, ScanErrorKind kind) =>
        FileProcessingResult.Failed(new ScanError(kind, path, exception.Message));

    private static bool IsFileAccessException(Exception exception) => exception is
        IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException;

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
}
