using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class PhpPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "php",
                new Dictionary<string, string> { [".php"] = "php", [".phtml"] = "php" },
                new Dictionary<string, string>
                {
                    ["composer.json"] = "json",
                    ["composer.lock"] = "json"
                });
    }
}
