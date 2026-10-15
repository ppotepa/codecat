namespace Codecat.Plugins;

public sealed class PluginRegistry(PluginSelection? selection = null) : IPluginRegistry
{
    public IReadOnlyList<ICodecatPlugin> GetPlugins() => BuiltInPluginCatalog.Create(selection ?? new PluginSelection());
}
