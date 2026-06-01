namespace Codecat.Minification;

internal interface IContentMinifier
{
    IReadOnlyCollection<string> Languages { get; }
    bool TryMinify(string content, out string minified);
}
