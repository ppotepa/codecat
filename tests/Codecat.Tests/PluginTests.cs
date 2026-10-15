using Codecat.Plugins;
using Codecat.Plugins.Matching;
using Xunit;

namespace Codecat.Tests;

public sealed class PluginTests
{
    [Fact]
    public void BuiltInCatalogHasUniquePluginIds()
    {
        var plugins = BuiltInPluginCatalog.Create();

        Assert.Equal(34, plugins.Count);
        Assert.Equal(plugins.Count, plugins.Select(plugin => plugin.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void LongestDeclaredSuffixWins()
    {
        var match = PluginMatcher.Match("src/example.app.src", BuiltInPluginCatalog.Create());

        Assert.NotNull(match);
        Assert.Equal("erlang", match.PluginName);
        Assert.Equal("suffix:.app.src", match.Reason);
    }

    [Fact]
    public void MarkdownIsOptIn()
    {
        Assert.Null(PluginMatcher.Match("README.md", BuiltInPluginCatalog.Create()));
        Assert.Equal("markdown", PluginMatcher.Match("README.md", BuiltInPluginCatalog.Create(new PluginSelection(IncludeDocumentation: true)))?.Language);
    }

    [Theory]
    [InlineData(".gitignore", "gitignore")]
    [InlineData(".editorconfig", "editorconfig")]
    [InlineData("module.app.src", "erlang")]
    public void DotFilesAndMultiPartRulesAreReachable(string path, string language)
    {
        Assert.Equal(language, PluginMatcher.Match(path, BuiltInPluginCatalog.Create())?.Language);
    }

    [Fact]
    public void MakefileHasAnExplicitShellOwner()
    {
        Assert.Equal("shell", PluginMatcher.Match("Makefile", BuiltInPluginCatalog.Create())?.PluginName);
    }
}
