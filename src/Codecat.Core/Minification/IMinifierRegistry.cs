namespace Codecat.Minification;

public interface IContentMinifier
{
    IReadOnlyCollection<string> Languages { get; }
    bool TryMinify(string content, out string minified);
}

public interface IMinifierRegistry
{
    bool TryMinify(string content, string language, out string minified);
}
