using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ObjectiveCPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "objective-c",
                new Dictionary<string, string>
                {
                    [".m"] = "objective-c",
                    [".mm"] = "objective-cpp"
                });
    }
}
