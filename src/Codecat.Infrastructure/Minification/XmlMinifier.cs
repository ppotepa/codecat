namespace Codecat.Minification;

public sealed class XmlMinifier : IContentMinifier
{
    public IReadOnlyCollection<string> Languages { get; } = ["xml"];

    public bool TryMinify(string content, out string minified)
    {
        minified = content;
        return false;
    }
}
