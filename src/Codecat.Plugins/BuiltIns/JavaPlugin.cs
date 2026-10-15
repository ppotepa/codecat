using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class JavaPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "java",
                new Dictionary<string, string>
                {
                    [".java"] = "java",
                    [".jsp"] = "jsp"
                },
                new Dictionary<string, string>
                {
                    ["pom.xml"] = "xml",
                    ["mvnw"] = "shell",
                    ["mvnw.cmd"] = "batch"
                });
    }
}
