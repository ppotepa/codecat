namespace Codecat.Plugins;

public interface IPluginRegistry
{
    IReadOnlyList<ICodecatPlugin> GetPlugins();
}
