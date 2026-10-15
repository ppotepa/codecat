using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class CPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "c",
                new Dictionary<string, string>
                {
                    [".c"] = "c",
                    [".h"] = "c"
                },
                new Dictionary<string, string> { ["Makefile"] = "makefile" });
    }
}
