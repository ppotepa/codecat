using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class PowershellPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "powershell",
                new Dictionary<string, string>
                {
                    [".ps1"] = "powershell",
                    [".psm1"] = "powershell",
                    [".psd1"] = "powershell",
                    [".ps1xml"] = "xml"
                });
    }
}
