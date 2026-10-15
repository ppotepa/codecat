using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ShellPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "shell",
                new Dictionary<string, string>
                {
                    [".sh"] = "shell",
                    [".bash"] = "shell",
                    [".zsh"] = "shell",
                    [".fish"] = "fish",
                    [".bat"] = "batch",
                    [".cmd"] = "batch"
                },
                new Dictionary<string, string>
                {
                    ["Dockerfile"] = "dockerfile",
                    ["Containerfile"] = "dockerfile",
                    ["Makefile"] = "makefile"
                },
                priority: 1);
    }
}
