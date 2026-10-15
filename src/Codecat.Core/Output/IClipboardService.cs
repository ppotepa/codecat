namespace Codecat.Output;

public interface IClipboardService
{
    ClipboardCopyResult CopyFile(string path);
}

public sealed record ClipboardCopyResult(bool Success, string Message)
{
    public static ClipboardCopyResult Copied(string tool) => new(true, $"copied output to clipboard using {tool}");
    public static ClipboardCopyResult Failed(string message) => new(false, message);
}
