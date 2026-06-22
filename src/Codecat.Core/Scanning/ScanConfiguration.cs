namespace Codecat.Scanning;

public sealed record ScanConfiguration
{
    public long DefaultMaxFileBytes { get; init; } = 250_000;
    public bool DefaultQuiet { get; init; } = false;
    public bool DefaultVerbose { get; init; } = false;
    public bool DefaultUseGitignore { get; init; } = false;
    public bool DefaultAllPlugins { get; init; } = false;
    public HashSet<string>? DefaultExtensionFilter { get; init; }
}
