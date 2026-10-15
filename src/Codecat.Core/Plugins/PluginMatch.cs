namespace Codecat.Plugins;

public sealed record PluginMatch(string PluginName, string Language, string Reason, int Priority = 0, int Specificity = 0);
