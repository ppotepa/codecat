using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class GradlePlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "gradle",
                new Dictionary<string, string>
                {
                    [".gradle"] = "gradle"
                },
                new Dictionary<string, string>
                {
                    ["build.gradle"] = "gradle",
                    ["build.gradle.kts"] = "kotlin",
                    ["settings.gradle"] = "gradle",
                    ["settings.gradle.kts"] = "kotlin",
                    ["gradle.properties"] = "properties",
                    ["gradle-wrapper.properties"] = "properties",
                    ["libs.versions.toml"] = "toml",
                    ["versions.toml"] = "toml"
                });
    }
}
