using System.Text;
using LzmaNet;

namespace Codecat.Output;

public static class XzBase64Writer
{
    private const string Header = "CODECAT-XZ-BASE64/1\n\n";

    public static void Write(string sourcePath, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var compressed = XzCompressor.Compress(
            File.ReadAllBytes(sourcePath),
            new XzCompressOptions { Preset = 9 });

        using var output = new StreamWriter(outputPath, false, Encoding.ASCII);
        output.Write(Header);
        output.Write(Convert.ToBase64String(compressed));
        output.Write('\n');
    }
}
