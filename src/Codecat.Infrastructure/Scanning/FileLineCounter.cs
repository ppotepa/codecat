using System.Text;

namespace Codecat.Scanning;

public sealed class FileLineCounter : IFileLineCounter
{
    public async Task<long> CountAsync(string path, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 16_384);
        var buffer = new char[16_384];
        long lines = 0;
        var hasContent = false;
        var previousWasCr = false;
        var lastWasNewline = false;
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            hasContent = true;
            foreach (var value in buffer.AsSpan(0, read))
            {
                if (value == '\r')
                {
                    lines++;
                    previousWasCr = true;
                    lastWasNewline = true;
                }
                else if (value == '\n')
                {
                    if (!previousWasCr) lines++;
                    previousWasCr = false;
                    lastWasNewline = true;
                }
                else
                {
                    previousWasCr = false;
                    lastWasNewline = false;
                }
            }
        }

        return lines + (hasContent && !lastWasNewline ? 1 : 0);
    }
}
