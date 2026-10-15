using Codecat.Plugins;

namespace Codecat.Plugins.Matching;

public static class PluginMatcher
{
    public static PluginMatch? Match(string relativePath, IReadOnlyList<ICodecatPlugin> plugins)
    {
        return MatchAll(relativePath, plugins).FirstOrDefault();
    }

    public static IReadOnlyList<PluginMatch> MatchAll(string relativePath, IReadOnlyList<ICodecatPlugin> plugins)
    {
        return plugins
            .Select((plugin, order) => (Match: plugin.TryMatchFile(relativePath), Order: order))
            .Where(candidate => candidate.Match is not null)
            .OrderByDescending(candidate => candidate.Match!.Priority)
            .ThenByDescending(candidate => candidate.Match!.Specificity)
            .ThenBy(candidate => candidate.Order)
            .Select(candidate => candidate.Match!)
            .ToArray();
    }
}
