namespace Codecat.Scanning;

public sealed record FileMetrics(int Lines, long Bytes, string Sha256, long OriginalBytes, int OriginalLines);

public interface IMetricsCalculator
{
    FileMetrics Calculate(string fullPath, string content, string originalContent);
}
