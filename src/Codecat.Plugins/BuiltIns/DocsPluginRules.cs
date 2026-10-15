namespace Codecat.Plugins.BuiltIns;

internal static class DocsPluginRules
{
    internal static Dictionary<string, string> CreateExtensions(bool includeAll)
    {
        var extensions = new Dictionary<string, string>
        {
            [".mdx"] = "mdx",
            [".rst"] = "rst",
            [".txt"] = "text",
            [".adoc"] = "asciidoc"
        };

        if (includeAll) extensions[".md"] = "markdown";
        return extensions;
    }

    internal static Dictionary<string, string> CreateExactFileNames(bool includeAll)
    {
        var names = new Dictionary<string, string>
        {
            ["README"] = "text",
            ["LICENSE"] = "text",
            ["CHANGELOG"] = "text"
        };
        if (includeAll) names["CHANGELOG.md"] = "markdown";
        return names;
    }
}
