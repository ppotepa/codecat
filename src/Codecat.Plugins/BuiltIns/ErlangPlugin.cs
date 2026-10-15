using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class ErlangPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
                "erlang",
                new Dictionary<string, string>
                {
                    [".erl"] = "erlang",
                    [".hrl"] = "erlang",
                    [".app.src"] = "erlang"
                },
                new Dictionary<string, string> { ["rebar.config"] = "erlang" });
    }
}
