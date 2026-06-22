using System.Text;

namespace Codecat.Minification;

public sealed class CssMinifier : IContentMinifier
{
    public IReadOnlyCollection<string> Languages { get; } = ["css", "scss", "sass", "less"];

    public bool TryMinify(string content, out string minified)
    {
        var withoutComments = RemoveBlockComments(content);
        var builder = new StringBuilder(withoutComments.Length);
        var previousWasWhitespace = false;

        foreach (var character in withoutComments)
        {
            if (char.IsWhiteSpace(character))
            {
                previousWasWhitespace = true;
                continue;
            }

            if (IsCssSeparator(character))
            {
                TrimTrailingSpace(builder);
                builder.Append(character);
                previousWasWhitespace = false;
                continue;
            }

            if (previousWasWhitespace && builder.Length > 0 && !IsCssSeparator(builder[^1]))
            {
                builder.Append(' ');
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        minified = builder.ToString().Trim();
        return minified.Length < content.Length;
    }

    private static string RemoveBlockComments(string content)
    {
        var builder = new StringBuilder(content.Length);
        var inComment = false;

        for (var i = 0; i < content.Length; i++)
        {
            if (!inComment && i + 1 < content.Length && content[i] == '/' && content[i + 1] == '*')
            {
                inComment = true;
                i++;
                continue;
            }

            if (inComment && i + 1 < content.Length && content[i] == '*' && content[i + 1] == '/')
            {
                inComment = false;
                i++;
                continue;
            }

            if (!inComment)
            {
                builder.Append(content[i]);
            }
        }

        return builder.ToString();
    }

    private static bool IsCssSeparator(char character)
    {
        return character is '{' or '}' or ':' or ';' or ',' or '>' or '+' or '~' or '(' or ')';
    }

    private static void TrimTrailingSpace(StringBuilder builder)
    {
        while (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }
    }
}
