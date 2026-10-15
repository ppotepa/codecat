using System.Collections.ObjectModel;

namespace Codecat.Plugins;

public sealed class ExtensionPlugin : ICodecatPlugin
{
    private readonly IReadOnlyDictionary<string, string> _extensions;
    private readonly IReadOnlyDictionary<string, string> _exactFileNames;
    private readonly string[] _orderedSuffixes;
    private readonly int _priority;

    public ExtensionPlugin(
        string name,
        IReadOnlyDictionary<string, string> extensions,
        IReadOnlyDictionary<string, string>? exactFileNames = null,
        int priority = 0)
    {
        Name = name;
        var extensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
        {
            extensionMap[extension.Key] = extension.Value;
        }

        var exactMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (exactFileNames is not null)
        {
            foreach (var fileName in exactFileNames)
            {
                exactMap[fileName.Key] = fileName.Value;
            }
        }

        _extensions = new ReadOnlyDictionary<string, string>(extensionMap);
        _exactFileNames = new ReadOnlyDictionary<string, string>(exactMap);
        _orderedSuffixes = extensionMap.Keys.OrderByDescending(value => value.Length)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        _priority = priority;

        IncludeRules = Array.AsReadOnly(_extensions.Keys.Concat(_exactFileNames.Keys).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public string Name { get; }
    public IReadOnlyCollection<string> IncludeRules { get; }
    public IReadOnlyDictionary<string, string> ExtensionRules => _extensions;
    public IReadOnlyDictionary<string, string> ExactFileRules => _exactFileNames;

    public PluginMatch? TryMatchFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        if (_exactFileNames.TryGetValue(fileName, out var exactLanguage))
        {
            return new PluginMatch(Name, exactLanguage, $"filename:{fileName}", 300 + _priority, fileName.Length);
        }

        foreach (var suffix in _orderedSuffixes)
        {
            if (relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                _extensions.TryGetValue(suffix, out var language))
            {
                var isExtension = string.Equals(suffix, Path.GetExtension(relativePath), StringComparison.OrdinalIgnoreCase);
                return new PluginMatch(Name, language, $"{(isExtension ? "extension" : "suffix")}:{suffix}", (isExtension ? 100 : 200) + _priority, suffix.Length);
            }
        }

        return null;
    }
}
