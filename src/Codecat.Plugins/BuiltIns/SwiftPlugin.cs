using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class SwiftPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "swift",
                new Dictionary<string, string> { [".swift"] = "swift" },
                new Dictionary<string, string> { ["Package.swift"] = "swift" });
    }
}
