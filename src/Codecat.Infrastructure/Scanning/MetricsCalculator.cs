using System.Text;

namespace Codecat.Scanning;

public sealed class MetricsCalculator : IMetricsCalculator
{
    public FileMetrics Calculate(string content, string originalContent, long originalBytes, string originalSha256)
    {
        return new FileMetrics(
            Lines: CountLines(content),
            Bytes: Encoding.UTF8.GetByteCount(content),
            Sha256: originalSha256,
            OriginalBytes: originalBytes,
            OriginalLines: CountLines(originalContent));
    }

    private static int CountLines(string content)
    {
        var newlines = 0;
        var previousWasCarriageReturn = false;
        foreach (var character in content)
        {
            if (character == '\r')
            {
                newlines++;
                previousWasCarriageReturn = true;
            }
            else if (character == '\n')
            {
                if (!previousWasCarriageReturn) newlines++;
                previousWasCarriageReturn = false;
            }
            else
            {
                previousWasCarriageReturn = false;
            }
        }

        return content.Length == 0 || content[^1] is '\r' or '\n' ? newlines : newlines + 1;
    }
}
