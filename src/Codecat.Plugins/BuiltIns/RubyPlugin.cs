using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class RubyPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "ruby",
                new Dictionary<string, string>
                {
                    [".rb"] = "ruby",
                    [".erb"] = "erb",
                    [".gemspec"] = "ruby"
                },
                new Dictionary<string, string>
                {
                    ["Gemfile"] = "ruby",
                    ["Gemfile.lock"] = "text",
                    ["Rakefile"] = "ruby"
                });
    }
}
