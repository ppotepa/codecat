using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class GoPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "go",
                new Dictionary<string, string> { [".go"] = "go" },
                new Dictionary<string, string>
                {
                    ["go.mod"] = "gomod",
                    ["go.sum"] = "gosum",
                    ["go.work"] = "gowork"
                });
    }
}
