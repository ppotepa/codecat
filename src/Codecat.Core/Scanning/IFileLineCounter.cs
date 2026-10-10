namespace Codecat.Scanning;

public interface IFileLineCounter
{
    Task<long> CountAsync(string path, CancellationToken cancellationToken);
}
