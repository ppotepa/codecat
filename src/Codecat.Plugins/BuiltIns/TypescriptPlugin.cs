using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class TypescriptPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "typescript",
                new Dictionary<string, string>
                {
                    [".ts"] = "typescript",
                    [".tsx"] = "typescript",
                    [".mts"] = "typescript",
                    [".cts"] = "typescript"
                },
                new Dictionary<string, string>
                {
                    ["tsconfig.json"] = "json",
                    ["vite.config.ts"] = "typescript",
                    ["next.config.ts"] = "typescript"
                });
    }
}
