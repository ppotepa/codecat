namespace Codecat.Scanning;

public static class GlobalDirectoryRules
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {

        ".git", ".hg", ".svn", ".vs", ".vscode", ".idea",
        "bin", "obj", "build", "dist", "out", "target", "coverage",
        "node_modules", ".next", ".nuxt", ".svelte-kit", ".parcel-cache",
        ".gradle", ".cxx", ".externalNativeBuild", "CMakeFiles", "cmake-build-debug", "cmake-build-release",
        ".venv", "venv", "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache",
        ".build", "DerivedData", ".dart_tool", ".flutter-plugins-dependencies", ".luarocks", ".Rproj.user",
        ".bsp", ".metals", "_build", "deps", ".stack-work", "dist-newstyle",
        ".bundle", "vendor", ".docusaurus", "_site", "site", ".codegraph"
    };

    public static IReadOnlyCollection<string> All => IgnoredDirectories.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    public static bool IsIgnored(string directoryName) => IgnoredDirectories.Contains(directoryName);
}
