using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class RPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "r",
                new Dictionary<string, string>
                {
                    [".r"] = "r",
                    [".R"] = "r",
                    [".Rmd"] = "rmarkdown"
                },
                new Dictionary<string, string>
                {
                    ["DESCRIPTION"] = "text",
                    ["NAMESPACE"] = "text"
                });
    }
}
