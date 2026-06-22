using System.Text;

namespace Codecat.Scanning;

public sealed class MetricsCalculator : IMetricsCalculator
{
    public FileMetrics Calculate(string fullPath, string content, string originalContent)
    {
        return new FileMetrics(
            Lines: CountLines(content),
            Bytes: Encoding.UTF8.GetByteCount(content),
            Sha256: FileReader.ComputeSha256(fullPath),
            OriginalBytes: Encoding.UTF8.GetByteCount(originalContent),
            OriginalLines: CountLines(originalContent));
    }

    private static int CountLines(string content)
    {
        if (content.Length == 0)
        {
            return 0;
        }

        var newlines = 0;
        foreach (var character in content)
        {
            if (character == '\n')
            {
                newlines++;
            }
        }

        return content[^1] == '\n' ? newlines : newlines + 1;
    }
}
