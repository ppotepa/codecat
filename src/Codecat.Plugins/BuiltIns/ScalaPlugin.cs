using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ScalaPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "scala",
                new Dictionary<string, string>
                {
                    [".scala"] = "scala",
                    [".sbt"] = "scala"
                },
                new Dictionary<string, string> { ["build.sbt"] = "scala" });
    }
}
