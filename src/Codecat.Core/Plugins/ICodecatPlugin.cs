namespace Codecat.Plugins;

public interface ICodecatPlugin
{
    string Name { get; }
    IReadOnlyCollection<string> IncludeRules { get; }
    IReadOnlyCollection<string> IgnoreDirectoryRules { get; }
    PluginMatch? TryMatchFile(string relativePath);
    bool ShouldIgnoreDirectory(string relativePath);
}
