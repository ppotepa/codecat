using System.Text;
using System.Text.RegularExpressions;

namespace Codecat.Scanning;

internal sealed class GitignoreRules
{
    private readonly IReadOnlyList<GitignoreRule> _rules;

    private GitignoreRules(IReadOnlyList<GitignoreRule> rules)
    {
        _rules = rules;
    }

    public static GitignoreRules Empty { get; } = new(Array.Empty<GitignoreRule>());

    public GitignoreRules AddFromFile(string path, string baseRelativePath)
    {
        var rules = _rules.ToList();
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            var rule = GitignoreRule.TryParse(baseRelativePath, line);
            if (rule is not null)
            {
                rules.Add(rule);
            }
        }

        return rules.Count == _rules.Count ? this : new GitignoreRules(rules);
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        var ignored = false;
        var normalizedPath = NormalizePath(relativePath);

        foreach (var rule in _rules)
        {
            if (rule.IsMatch(normalizedPath, isDirectory))
            {
                ignored = !rule.Negated;
            }
        }

        return ignored;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized == "." ? string.Empty : normalized.TrimStart('/');
    }

    private sealed class GitignoreRule
    {
        private static readonly RegexOptions PathRegexOptions =
            RegexOptions.CultureInvariant | (OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase : RegexOptions.None);

        private readonly bool _directoryOnly;
        private readonly Regex _selfPattern;
        private readonly Regex _contentsPattern;

        private GitignoreRule(bool negated, bool directoryOnly, string pathPattern)
        {
            Negated = negated;
            _directoryOnly = directoryOnly;
            _selfPattern = new Regex($"^{pathPattern}$", PathRegexOptions);
            _contentsPattern = new Regex($"^{pathPattern}/.*$", PathRegexOptions);
        }

        public bool Negated { get; }

        public static GitignoreRule? TryParse(string baseRelativePath, string rawLine)
        {
            var line = TrimUnescapedTrailingSpaces(rawLine.TrimEnd('\r'));
            if (line.Length == 0)
            {
                return null;
            }

            if (line[0] == '#')
            {
                return null;
            }

            var negated = false;
            if (line[0] == '!')
            {
                negated = true;
                line = TrimUnescapedTrailingSpaces(line[1..]);
                if (line.Length == 0)
                {
                    return null;
                }
            }
            else if (line.Length > 1 && line[0] == '\\' && (line[1] == '#' || line[1] == '!'))
            {
                line = line[1..];
            }

            var directoryOnly = line.EndsWith("/", StringComparison.Ordinal);
            while (line.EndsWith("/", StringComparison.Ordinal))
            {
                line = line[..^1];
            }

            var anchored = line.StartsWith("/", StringComparison.Ordinal);
            while (line.StartsWith("/", StringComparison.Ordinal))
            {
                line = line[1..];
            }

            if (line.Length == 0)
            {
                return null;
            }

            var basePrefix = FormatBasePrefix(baseRelativePath);
            var hasSlash = line.Contains('/', StringComparison.Ordinal);
            var pathPattern = anchored || hasSlash
                ? basePrefix + GlobToRegex(line)
                : basePrefix + "(?:.*/)?" + GlobToRegex(line);

            return new GitignoreRule(negated, directoryOnly, pathPattern);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            if (_directoryOnly)
            {
                return isDirectory
                    ? _selfPattern.IsMatch(relativePath)
                    : _contentsPattern.IsMatch(relativePath);
            }

            return _selfPattern.IsMatch(relativePath) || _contentsPattern.IsMatch(relativePath);
        }

        private static string FormatBasePrefix(string baseRelativePath)
        {
            var normalized = NormalizePath(baseRelativePath);
            return normalized.Length == 0 ? string.Empty : Regex.Escape(normalized) + "/";
        }

        private static string GlobToRegex(string pattern)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < pattern.Length; i++)
            {
                var character = pattern[i];
                switch (character)
                {
                    case '\\':
                        if (i + 1 < pattern.Length)
                        {
                            AppendRegexLiteral(builder, pattern[++i]);
                        }
                        else
                        {
                            AppendRegexLiteral(builder, character);
                        }

                        break;

                    case '*':
                        if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                        {
                            if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                            {
                                builder.Append("(?:.*/)?");
                                i += 2;
                            }
                            else
                            {
                                builder.Append(".*");
                                i++;
                            }
                        }
                        else
                        {
                            builder.Append("[^/]*");
                        }

                        break;

                    case '?':
                        builder.Append("[^/]");
                        break;

                    case '[':
                        AppendCharacterClass(builder, pattern, ref i);
                        break;

                    default:
                        AppendRegexLiteral(builder, character);
                        break;
                }
            }

            return builder.ToString();
        }

        private static void AppendCharacterClass(StringBuilder builder, string pattern, ref int index)
        {
            var end = index + 1;
            if (end < pattern.Length && (pattern[end] == '!' || pattern[end] == '^'))
            {
                end++;
            }

            if (end < pattern.Length && pattern[end] == ']')
            {
                end++;
            }

            for (; end < pattern.Length; end++)
            {
                if (pattern[end] == '\\' && end + 1 < pattern.Length)
                {
                    end++;
                    continue;
                }

                if (pattern[end] == ']')
                {
                    break;
                }
            }

            if (end >= pattern.Length)
            {
                AppendRegexLiteral(builder, '[');
                return;
            }

            builder.Append('[');
            var start = index + 1;
            if (pattern[start] == '!')
            {
                builder.Append('^');
                start++;
            }
            else if (pattern[start] == '^')
            {
                builder.Append("\\^");
                start++;
            }

            for (var i = start; i < end; i++)
            {
                var character = pattern[i];
                if (character == '\\' && i + 1 < end)
                {
                    AppendCharacterClassLiteral(builder, pattern[++i]);
                    continue;
                }

                AppendCharacterClassCharacter(builder, character);
            }

            builder.Append(']');
            index = end;
        }

        private static string TrimUnescapedTrailingSpaces(string value)
        {
            var end = value.Length;
            while (end > 0 && value[end - 1] == ' ' && !IsEscaped(value, end - 1))
            {
                end--;
            }

            return end == value.Length ? value : value[..end];
        }

        private static bool IsEscaped(string value, int index)
        {
            var backslashes = 0;
            for (var i = index - 1; i >= 0 && value[i] == '\\'; i--)
            {
                backslashes++;
            }

            return backslashes % 2 == 1;
        }

        private static void AppendRegexLiteral(StringBuilder builder, char character)
        {
            if ("\\.^$|?*+()[]{}".Contains(character, StringComparison.Ordinal))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        private static void AppendCharacterClassCharacter(StringBuilder builder, char character)
        {
            if (character is '\\' or ']')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        private static void AppendCharacterClassLiteral(StringBuilder builder, char character)
        {
            if (character is '\\' or ']' or '-' or '^')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }
    }
}
