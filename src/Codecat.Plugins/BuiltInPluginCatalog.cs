using Codecat.Plugins.BuiltIns;
using Codecat.Plugins.Matching;

namespace Codecat.Plugins;

public static class BuiltInPluginCatalog
{
    public static IReadOnlyList<ICodecatPlugin> Create(PluginSelection? selection = null)
    {
        var plugins = new ICodecatPlugin[]
        {
            CsharpPlugin.Create(),
            JavaPlugin.Create(),
            GradlePlugin.Create(),
            KotlinPlugin.Create(),
            AndroidPlugin.Create(),
            SwiftPlugin.Create(),
            ObjectiveCPlugin.Create(),
            CppPlugin.Create(),
            CPlugin.Create(),
            RustPlugin.Create(),
            GoPlugin.Create(),
            PythonPlugin.Create(),
            JavascriptPlugin.Create(),
            TypescriptPlugin.Create(),
            WebPlugin.Create(),
            ShaderPlugin.Create(),
            ScriptPlugin.Create(),
            PhpPlugin.Create(),
            RubyPlugin.Create(),
            DartFlutterPlugin.Create(),
            LuaPlugin.Create(),
            RPlugin.Create(),
            JuliaPlugin.Create(),
            ScalaPlugin.Create(),
            ElixirPlugin.Create(),
            ErlangPlugin.Create(),
            HaskellPlugin.Create(),
            FsharpPlugin.Create(),
            PowershellPlugin.Create(),
            ShellPlugin.Create(),
            SqlPlugin.Create(),
            ConfigPlugin.Create(),
            TomlPlugin.Create(),
            DocsPlugin.Create(selection?.IncludeAllSources ?? false, selection?.IncludeDocumentation ?? false)
        };

        var duplicate = plugins.GroupBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate plugin id: {duplicate.Key}");
        ValidateRuleConflicts(plugins);
        return plugins;
    }

    private static void ValidateRuleConflicts(IReadOnlyList<ICodecatPlugin> plugins)
    {
        var candidates = plugins.SelectMany(plugin => plugin.ExactFileRules.Keys
                .Concat(plugin.ExtensionRules.Keys.Select(suffix => $"sample{suffix}")))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var matches = PluginMatcher.MatchAll(candidate, plugins);
            if (matches.Count > 1 && matches[0].Priority == matches[1].Priority && matches[0].Specificity == matches[1].Specificity)
            {
                throw new InvalidOperationException(
                    $"Ambiguous built-in plugin rules for '{candidate}': {matches[0].PluginName} and {matches[1].PluginName} have equal priority.");
            }
        }
    }
}
