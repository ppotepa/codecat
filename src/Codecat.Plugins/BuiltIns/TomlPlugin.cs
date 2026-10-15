using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class TomlPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "toml",
                new Dictionary<string, string>
                {
                    [".toml"] = "toml"
                });
    }
}
