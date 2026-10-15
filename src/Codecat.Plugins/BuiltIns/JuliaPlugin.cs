using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class JuliaPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "julia",
                new Dictionary<string, string> { [".jl"] = "julia" },
                new Dictionary<string, string>
                {
                    ["Project.toml"] = "toml",
                    ["Manifest.toml"] = "toml"
                });
    }
}
