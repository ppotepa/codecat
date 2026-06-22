

namespace Codecat.Plugins;

public sealed class ExtensionPlugin : ICodecatPlugin
{
    private readonly IReadOnlyDictionary<string, string> _extensions;
    private readonly IReadOnlyDictionary<string, string> _exactFileNames;
    private readonly HashSet<string> _ignoredDirectories;

    public ExtensionPlugin(
        string name,
        IReadOnlyDictionary<string, string> extensions,
        IReadOnlyDictionary<string, string>? exactFileNames = null,
        IEnumerable<string>? ignoredDirectories = null)
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

        _extensions = extensionMap;
        _exactFileNames = exactMap;

        _ignoredDirectories = new HashSet<string>(ignoredDirectories ?? [], StringComparer.OrdinalIgnoreCase);
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
