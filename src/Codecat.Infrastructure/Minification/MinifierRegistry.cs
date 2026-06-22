namespace Codecat.Minification;

public sealed class MinifierRegistry : IMinifierRegistry
{
    private readonly IReadOnlyList<IContentMinifier> _minifiers;

    public MinifierRegistry()
    {
        _minifiers = new List<IContentMinifier>
        {
            new JsonMinifier(),
            new XmlMinifier(),
            new CssMinifier()
        };
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
