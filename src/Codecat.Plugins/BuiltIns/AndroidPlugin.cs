using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class AndroidPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "android",
                new Dictionary<string, string>
                {
                    [".aidl"] = "aidl",
                    [".xml"] = "xml",
                    [".pro"] = "proguard"
                },
                new Dictionary<string, string>
                {
                    ["AndroidManifest.xml"] = "xml",
                    ["Android.mk"] = "makefile",
                    ["Application.mk"] = "makefile",
                    ["proguard-rules.pro"] = "proguard",
                    ["consumer-rules.pro"] = "proguard",
                    ["network_security_config.xml"] = "xml"
                });
    }
}
