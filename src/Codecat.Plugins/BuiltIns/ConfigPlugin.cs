using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ConfigPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "config",
                new Dictionary<string, string>
                {
                    [".json"] = "json",
                    [".ftl"] = "fluent",
                    [".yaml"] = "yaml",
                    [".yml"] = "yaml",
                    [".ini"] = "ini",
                    [".conf"] = "conf",
                    [".config"] = "xml",
                    [".graphql"] = "graphql",
                    [".gql"] = "graphql",
                    [".http"] = "http",
                    [".rest"] = "http",
                    [".editorconfig"] = "editorconfig",
                    [".gitignore"] = "gitignore",
                    [".gitattributes"] = "gitattributes",
                    [".gitmodules"] = "gitmodules",
                    [".codegraphignore"] = "gitignore"
                },
                new Dictionary<string, string>
                {
                    [".env.example"] = "dotenv",
                    ["appsettings.json"] = "json",
                    ["appsettings.Development.json"] = "json",
                    ["codegraph.json"] = "json",
                    ["codegraph.toml"] = "toml"
                });
    }
}
