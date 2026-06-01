namespace Codecat.Minification;

internal sealed class MinifierRegistry
{
    private readonly IReadOnlyList<IContentMinifier> _minifiers;

    private MinifierRegistry(IReadOnlyList<IContentMinifier> minifiers)
    {
        _minifiers = minifiers;
    }

    public static MinifierRegistry CreateDefault()
    {
        return new MinifierRegistry(
        [
            new JsonMinifier(),
            new XmlMinifier(),
            new CssMinifier()
        ]);
    }

    public bool TryMinify(string content, string language, out string minified)
    {
        foreach (var minifier in _minifiers)
        {
            if (minifier.Languages.Contains(language, StringComparer.OrdinalIgnoreCase) &&
                minifier.TryMinify(content, out minified))
            {
                return true;
            }
        }

        minified = content;
        return false;
    }
}
