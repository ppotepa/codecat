namespace Codecat.Scanning;

public sealed record FileMetrics(int Lines, long Bytes, string Sha256, long OriginalBytes, int OriginalLines);

public interface IMetricsCalculator
{
    FileMetrics Calculate(string content, string originalContent, long originalBytes, string originalSha256);
}
