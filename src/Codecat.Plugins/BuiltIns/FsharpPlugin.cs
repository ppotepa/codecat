using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class FsharpPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "fsharp",
                new Dictionary<string, string>
                {
                    [".fs"] = "fsharp",
                    [".fsi"] = "fsharp",
                    [".fsx"] = "fsharp",
                    [".fsproj"] = "xml"
                });
    }
}
