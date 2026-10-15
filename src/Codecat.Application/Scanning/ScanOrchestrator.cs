using Microsoft.Extensions.Logging;
using Codecat.Plugins;

namespace Codecat.Scanning;

public sealed class ScanOrchestrator(
    IPluginRegistry pluginRegistry,
    FileProcessor fileProcessor,
    IErrorHandler<ScanError> errorHandler,
    ILogger<FileTreeWalker> logger) : IScanOrchestrator
{
    private readonly FileTreeWalker _walker = new(pluginRegistry, fileProcessor, errorHandler, logger);

    public async Task<ScanResult> ScanAsync(
        string root,
        string? outputPath,
        ScanOptions options,
        Action<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var session = new ScanSession();
        await _walker.WalkAsync(root, outputPath, options, session, progress, cancellationToken);
        return session.Complete();
    }
}
