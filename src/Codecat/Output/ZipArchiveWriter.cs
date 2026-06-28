using System.IO.Compression;

namespace Codecat.Output;

internal static class ZipArchiveWriter
{
    public static void WriteSingleFileArchive(string sourcePath, string zipPath, string entryName)
    {
        var directory = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
        var entry = archive.CreateEntry(entryName, CompressionLevel.SmallestSize);
        entry.LastWriteTime = File.GetLastWriteTime(sourcePath);

        using var entryStream = entry.Open();
        using var sourceStream = File.OpenRead(sourcePath);
        sourceStream.CopyTo(entryStream);
    }
}
