namespace Codecat.Plugins;

internal interface ICodecatPlugin
{
    string Name { get; }
    IReadOnlyCollection<string> IncludeRules { get; }
    IReadOnlyCollection<string> IgnoreDirectoryRules { get; }
    string? TryGetLanguage(string relativePath);
    bool ShouldIncludeFile(string relativePath);
    bool ShouldIgnoreDirectory(string relativePath);
}
