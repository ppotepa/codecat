using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class DocsPlugin
{
    internal static ICodecatPlugin Create(bool includeAll = false, bool includeDocs = false)
    {
        var includeMarkdown = includeAll || includeDocs;
        return new ExtensionPlugin(
            "docs",
            DocsPluginRules.CreateExtensions(includeMarkdown),
            DocsPluginRules.CreateExactFileNames(includeMarkdown));
    }
}
