using System.Runtime.InteropServices;

namespace Codecat.Output;

internal static class ClipboardEnvironmentProbe
{
    public static ClipboardEnvironment Capture()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var canWriteDevTty = CanWriteDevTty(isWindows);
        var stdoutRedirected = Console.IsOutputRedirected;
        var term = Environment.GetEnvironmentVariable("TERM") ?? string.Empty;

        return new ClipboardEnvironment(
            Os: DetectOs(isWindows, isMacOS, isLinux),
            LinuxDistribution: isLinux ? DetectLinuxDistribution() : "none",
            IsWindows: isWindows,
            IsMacOS: isMacOS,
            IsLinux: isLinux,
            IsTmux: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TMUX")),
            IsSsh: IsSshSession(),
            HasConsole: HasConsole(isWindows, term, stdoutRedirected, canWriteDevTty),
            CanWriteDevTty: canWriteDevTty,
            IsStdoutRedirected: stdoutRedirected,
            HasWayland: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")),
            HasX11: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")),
            Term: term,
            Shell: Environment.GetEnvironmentVariable("SHELL") ?? string.Empty);
    }

    public static void Print(ClipboardEnvironment environment, IReadOnlyList<string> strategies, TextWriter writer)
    {
        writer.WriteLine("environment:");
        writer.WriteLine($"  os: {environment.Os}");
        writer.WriteLine($"  linux_distribution: {environment.LinuxDistribution}");
        writer.WriteLine($"  platform: {environment.Platform}");
        writer.WriteLine($"  console: {FormatBool(environment.HasConsole)}");
        writer.WriteLine($"  tmux: {FormatBool(environment.IsTmux)}");
        writer.WriteLine($"  ssh: {FormatBool(environment.IsSsh)}");
        writer.WriteLine($"  term: {FormatValue(environment.Term)}");
        writer.WriteLine($"  shell: {FormatValue(environment.Shell)}");
        writer.WriteLine($"  stdout_redirected: {FormatBool(environment.IsStdoutRedirected)}");
        writer.WriteLine($"  dev_tty_writable: {FormatBool(environment.CanWriteDevTty)}");
        writer.WriteLine($"  wayland: {FormatBool(environment.HasWayland)}");
        writer.WriteLine($"  x11: {FormatBool(environment.HasX11)}");
        writer.WriteLine("clipboard_strategies:");

        if (strategies.Count == 0)
        {
            writer.WriteLine("  none");
            return;
        }

        for (var i = 0; i < strategies.Count; i++)
        {
            writer.WriteLine($"  {i + 1}. {strategies[i]}");
        }
    }

    private static string DetectOs(bool isWindows, bool isMacOS, bool isLinux)
    {
        if (isWindows)
        {
            return "windows";
        }

        if (isMacOS)
        {
            return "macos";
        }

        if (isLinux)
        {
            return "linux";
        }

        return "unknown";
    }

    private static string DetectLinuxDistribution()
    {
        const string osReleasePath = "/etc/os-release";
        if (!File.Exists(osReleasePath))
        {
            return "unknown";
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(osReleasePath))
        {
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator];
            var value = line[(separator + 1)..].Trim().Trim('"');
            values[key] = value;
        }

        values.TryGetValue("ID", out var id);
        values.TryGetValue("ID_LIKE", out var idLike);
        id = NormalizeDistributionToken(id);
        idLike = NormalizeDistributionToken(idLike);

        if (id is "ubuntu" or "debian" or "fedora")
        {
            return id;
        }

        if (ContainsDistributionToken(idLike, "ubuntu"))
        {
            return "ubuntu";
        }

        if (ContainsDistributionToken(idLike, "debian"))
        {
            return "debian";
        }

        if (ContainsDistributionToken(idLike, "fedora"))
        {
            return "fedora";
        }

        return string.IsNullOrWhiteSpace(id) ? "unknown" : id;
    }

    private static string NormalizeDistributionToken(string? value)
    {
        return (value ?? string.Empty).Trim().Trim('"').ToLowerInvariant();
    }

    private static bool ContainsDistributionToken(string value, string token)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(token, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSshSession()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SSH_TTY"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SSH_CONNECTION"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SSH_CLIENT"));
    }

    private static bool HasConsole(bool isWindows, string term, bool stdoutRedirected, bool canWriteDevTty)
    {
        if (isWindows)
        {
            return !stdoutRedirected;
        }

        if (string.IsNullOrWhiteSpace(term) || string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return canWriteDevTty || !stdoutRedirected;
    }

    private static bool CanWriteDevTty(bool isWindows)
    {
        if (isWindows)
        {
            return false;
        }

        try
        {
            using var tty = File.Open("/dev/tty", FileMode.Open, FileAccess.Write);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    private static string FormatBool(bool value)
    {
        return value ? "yes" : "no";
    }

    private static string FormatValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}

internal sealed record ClipboardEnvironment(
    string Os,
    string LinuxDistribution,
    bool IsWindows,
    bool IsMacOS,
    bool IsLinux,
    bool IsTmux,
    bool IsSsh,
    bool HasConsole,
    bool CanWriteDevTty,
    bool IsStdoutRedirected,
    bool HasWayland,
    bool HasX11,
    string Term,
    string Shell)
{
    public string Platform => IsLinux ? $"{Os}-{LinuxDistribution}" : Os;
}
