using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class SqlPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "sql",
                new Dictionary<string, string>
                {
                    [".sql"] = "sql",
                    [".psql"] = "sql",
                    [".mysql"] = "sql"
                });
    }
}
