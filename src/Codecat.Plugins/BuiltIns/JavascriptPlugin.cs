using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class JavascriptPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "javascript",
                new Dictionary<string, string>
                {
                    [".js"] = "javascript",
                    [".jsx"] = "javascript",
                    [".mjs"] = "javascript",
                    [".cjs"] = "javascript"
                },
                new Dictionary<string, string>
                {
                    ["package.json"] = "json",
                    ["package-lock.json"] = "json",
                    ["vite.config.js"] = "javascript",
                    ["next.config.js"] = "javascript",
                    ["webpack.config.js"] = "javascript"
                });
    }
}
