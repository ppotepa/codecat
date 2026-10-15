using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class WebPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "web",
                new Dictionary<string, string>
                {
                    [".html"] = "html",
                    [".htm"] = "html",
                    [".css"] = "css",
                    [".scss"] = "scss",
                    [".sass"] = "sass",
                    [".less"] = "less"
                });
    }
}
