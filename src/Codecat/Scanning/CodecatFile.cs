namespace Codecat.Scanning;

internal sealed record CodecatFile(
    string RelativePath,
    string Plugin,
    string Language,
    string Reason,
    long Bytes,
    int Lines,
    long OriginalBytes,
    int OriginalLines,
    bool Minified,
    string Sha256,
    string Content);
