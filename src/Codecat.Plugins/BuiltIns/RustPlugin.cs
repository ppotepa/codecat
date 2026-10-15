using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class RustPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "rust",
                new Dictionary<string, string> { [".rs"] = "rust", [".ron"] = "ron" },
                new Dictionary<string, string>
                {
                    ["Cargo.toml"] = "toml",
                    ["Cargo.lock"] = "toml",
                    ["rust-toolchain"] = "toml",
                    ["rust-toolchain.toml"] = "toml",
                    ["rustfmt.toml"] = "toml",
                    ["clippy.toml"] = "toml",
                    ["Cross.toml"] = "toml",
                    ["deny.toml"] = "toml",
                    ["tarpaulin.toml"] = "toml",
                    ["nextest.toml"] = "toml"
                });
    }
}
