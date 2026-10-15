namespace Codecat.Plugins;

public sealed record PluginSelection(bool IncludeAllSources = false, bool IncludeDocumentation = false)
{
    public bool IncludeMarkdown => IncludeAllSources || IncludeDocumentation;
}
