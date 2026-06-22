namespace Codecat.Scanning;

public interface IScanResultWriter
{
    void Write(string root, string outputPath, ScanResult result, bool mini);
}
