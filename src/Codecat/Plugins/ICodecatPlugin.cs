namespace Codecat.Plugins;

internal interface ICodecatPlugin
{
    string Name { get; }
    string? TryGetLanguage(string relativePath);
    bool ShouldIncludeFile(string relativePath);
    bool ShouldIgnoreDirectory(string relativePath);
}
