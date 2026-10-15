using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class CsharpPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "csharp",
                new Dictionary<string, string>
                {
                    [".cs"] = "csharp",
                    [".csproj"] = "xml",
                    [".sln"] = "text",
                    [".slnx"] = "xml",
                    [".props"] = "xml",
                    [".targets"] = "xml"
                });
    }
}
