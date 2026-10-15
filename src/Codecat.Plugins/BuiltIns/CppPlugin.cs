using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class CppPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "cpp",
                new Dictionary<string, string>
                {
                    [".cc"] = "cpp",
                    [".cpp"] = "cpp",
                    [".cxx"] = "cpp",
                    [".hh"] = "cpp",
                    [".hpp"] = "cpp",
                    [".hxx"] = "cpp",
                    [".ipp"] = "cpp",
                    [".ixx"] = "cpp",
                    [".cuh"] = "cpp",
                    [".cu"] = "cpp",
                    [".cmake"] = "cmake",
                    [".mk"] = "makefile"
                },
                new Dictionary<string, string>
                {
                    ["CMakeLists.txt"] = "cmake",
                    ["CMakePresets.json"] = "json",
                    ["CMakeUserPresets.json"] = "json"
                });
    }
}
