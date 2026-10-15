using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class HaskellPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "haskell",
                new Dictionary<string, string>
                {
                    [".hs"] = "haskell",
                    [".lhs"] = "haskell",
                    [".cabal"] = "cabal"
                },
                new Dictionary<string, string>
                {
                    ["stack.yaml"] = "yaml",
                    ["package.yaml"] = "yaml"
                });
    }
}
