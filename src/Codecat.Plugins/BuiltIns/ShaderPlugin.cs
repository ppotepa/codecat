using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ShaderPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "shader",
                new Dictionary<string, string>
                {
                    [".wgsl"] = "wgsl"
                });
    }
}
