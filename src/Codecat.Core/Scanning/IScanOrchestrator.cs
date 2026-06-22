namespace Codecat.Scanning;

public interface IScanOrchestrator
{
    Task<ScanResult> ScanAsync(string root, string outputPath, ScanOptions options, Action<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
}
