using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ElixirPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "elixir",
                new Dictionary<string, string>
                {
                    [".ex"] = "elixir",
                    [".exs"] = "elixir"
                },
                new Dictionary<string, string>
                {
                    ["mix.exs"] = "elixir",
                    ["mix.lock"] = "elixir"
                });
    }
}
