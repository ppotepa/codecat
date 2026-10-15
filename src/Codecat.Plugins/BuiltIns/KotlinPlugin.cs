using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class KotlinPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "kotlin",
                new Dictionary<string, string>
                {
                    [".kt"] = "kotlin",
                    [".kts"] = "kotlin"
                });
    }
}
