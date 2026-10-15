using System.Text;

namespace Codecat.Minification;

public sealed class CssMinifier : IContentMinifier
{
    public IReadOnlyCollection<string> Languages { get; } = ["css", "scss", "sass", "less"];

    public bool TryMinify(string content, out string minified)
    {
        minified = content;
        return false;
    }
}
