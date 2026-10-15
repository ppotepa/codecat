using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ScriptPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "script",
                new Dictionary<string, string>
                {
                    [".rhai"] = "rhai"
                });
    }
}
