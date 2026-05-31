using System.Collections.Frozen;

namespace Codecat.Plugins;

internal sealed class ExtensionPlugin : ICodecatPlugin
{
    private readonly FrozenDictionary<string, string> _extensions;
    private readonly FrozenDictionary<string, string> _exactFileNames;
    private readonly FrozenSet<string> _ignoredDirectories;

    public ExtensionPlugin(
        string name,
        IReadOnlyDictionary<string, string> extensions,
        IReadOnlyDictionary<string, string>? exactFileNames = null,
        IEnumerable<string>? ignoredDirectories = null)
    {
        Name = name;
        _extensions = extensions.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _exactFileNames = (exactFileNames ?? new Dictionary<string, string>()).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _ignoredDirectories = (ignoredDirectories ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        IncludeRules = _extensions.Keys.Concat(_exactFileNames.Keys).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        IgnoreDirectoryRules = _ignoredDirectories.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string Name { get; }
    public IReadOnlyCollection<string> IncludeRules { get; }
    public IReadOnlyCollection<string> IgnoreDirectoryRules { get; }

    public PluginMatch? TryMatchFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        if (_exactFileNames.TryGetValue(fileName, out var exactLanguage))
        {
            return new PluginMatch(Name, exactLanguage, $"filename:{fileName}");
        }

        var extension = Path.GetExtension(relativePath);
        return _extensions.TryGetValue(extension, out var language)
            ? new PluginMatch(Name, language, $"extension:{extension}")
            : null;
    }

    public bool ShouldIgnoreDirectory(string relativePath)
    {
        var directoryName = Path.GetFileName(relativePath);
        return _ignoredDirectories.Contains(directoryName);
    }
}
