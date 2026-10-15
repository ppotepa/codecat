namespace Codecat.Plugins;

public interface ICodecatPlugin
{
    string Name { get; }
    IReadOnlyCollection<string> IncludeRules { get; }
    IReadOnlyDictionary<string, string> ExtensionRules { get; }
    IReadOnlyDictionary<string, string> ExactFileRules { get; }
    PluginMatch? TryMatchFile(string relativePath);
}
