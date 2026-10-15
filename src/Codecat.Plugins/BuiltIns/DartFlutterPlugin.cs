using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class DartFlutterPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "dart-flutter",
                new Dictionary<string, string>
                {
                    [".dart"] = "dart",
                    [".arb"] = "json"
                },
                new Dictionary<string, string>
                {
                    ["pubspec.yaml"] = "yaml",
                    ["pubspec.lock"] = "yaml",
                    ["pubspec_overrides.yaml"] = "yaml",
                    ["analysis_options.yaml"] = "yaml",
                    ["build.yaml"] = "yaml",
                    ["melos.yaml"] = "yaml",
                    ["flutter_native_splash.yaml"] = "yaml",
                    ["flutter_launcher_icons.yaml"] = "yaml"
                });
    }
}
