using Codecat.Scanning;
using Microsoft.Extensions.Configuration;

namespace Codecat.Cli.Configuration;

public static class ScanConfigurationLoader
{
    public static ScanConfiguration Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("scan");
        var defaults = new ScanConfiguration();
        return new ScanConfiguration
        {
            DefaultMaxFileBytes = ReadLong(section, nameof(ScanConfiguration.DefaultMaxFileBytes), defaults.DefaultMaxFileBytes),
            DefaultQuiet = ReadBool(section, nameof(ScanConfiguration.DefaultQuiet), defaults.DefaultQuiet),
            DefaultVerbose = ReadBool(section, nameof(ScanConfiguration.DefaultVerbose), defaults.DefaultVerbose),
            DefaultUseGitignore = ReadBool(section, nameof(ScanConfiguration.DefaultUseGitignore), defaults.DefaultUseGitignore),
            DefaultAllPlugins = ReadBool(section, nameof(ScanConfiguration.DefaultAllPlugins), defaults.DefaultAllPlugins),
            DefaultExtensionFilter = ReadExtensions(section.GetSection(nameof(ScanConfiguration.DefaultExtensionFilter)))
        };
    }

    private static long ReadLong(IConfigurationSection section, string name, long fallback)
    {
        var value = section[name];
        if (value is null) return fallback;
        return long.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : throw new FormatException($"Configuration 'scan:{name}' must be a positive integer.");
    }

    private static bool ReadBool(IConfigurationSection section, string name, bool fallback)
    {
        var value = section[name];
        if (value is null) return fallback;
        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw new FormatException($"Configuration 'scan:{name}' must be true or false.");
    }

    private static HashSet<string>? ReadExtensions(IConfigurationSection section)
    {
        var values = section.Value is { } scalar
            ? scalar.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : section.GetChildren().Select(child => child.Value).Where(value => !string.IsNullOrWhiteSpace(value));
        var parsed = values.Select(value => value!.Trim())
            .Where(value => value.Length > 0)
            .Select(value => value.StartsWith('.') ? value : $".{value}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return parsed.Count == 0 ? null : parsed;
    }
}
