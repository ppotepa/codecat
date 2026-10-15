using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class LuaPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "lua",
                new Dictionary<string, string>
                {
                    [".lua"] = "lua",
                    [".rockspec"] = "lua"
                });
    }
}
