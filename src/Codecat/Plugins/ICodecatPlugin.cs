namespace Codecat.Plugins;

internal interface ICodecatPlugin
{
    string Name { get; }
    IReadOnlyCollection<string> IncludeRules { get; }
    IReadOnlyCollection<string> IgnoreDirectoryRules { get; }
    PluginMatch? TryMatchFile(string relativePath);
    bool ShouldIgnoreDirectory(string relativePath);
}
