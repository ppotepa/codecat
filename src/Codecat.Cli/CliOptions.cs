using System.Reflection;
using Codecat.Plugins;
using Codecat.Scanning;

namespace Codecat.Cli;

public sealed record CliOptions(
    string RootPath,
    string OutputPath,
    long MaxFileBytes,
    IReadOnlyCollection<string>? ExtensionFilter,
    bool Quiet,
    bool Verbose,
    bool ListPlugins,
    bool Mini,
    bool All,
    bool UseGitignore,
    bool CopyToClipboard,
    bool EnvProbe)
{
    public const long DefaultMaxFileBytes = 250_000;

    public static CliOptions? Parse(string[] args, ScanConfiguration configuration)
    {
        var root = ".";
        var extensionFilter = configuration.DefaultExtensionFilter is null
            ? null
            : new HashSet<string>(configuration.DefaultExtensionFilter, StringComparer.OrdinalIgnoreCase);
        var output = "concat.txt";
        var maxFileBytes = configuration.DefaultMaxFileBytes;
        var quiet = configuration.DefaultQuiet;
        var verbose = configuration.DefaultVerbose;
        var listPlugins = false;
        var mini = false;
        var all = false;
        var useGitignore = configuration.DefaultUseGitignore;
        var copyToClipboard = true;
        var envProbe = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                return null!;
            }

            if (arg is "--quiet" or "-q")
            {
                quiet = true;
                continue;
            }

            if (arg is "--verbose" or "-v")
            {
                verbose = true;
                continue;
            }

            if (arg is "--list-plugins")
            {
                listPlugins = true;
                continue;
            }

            if (arg is "--mini")
            {
                mini = true;
                continue;
            }

            if (arg is "--all")
            {
                all = true;
                continue;
            }

            if (arg is "--use-gitignore" or "--gitignore")
            {
                useGitignore = true;
                continue;
            }

            if (arg is "--copy" or "--clipboard")
            {
                copyToClipboard = true;
                continue;
            }

            if (arg is "--no-copy" or "--no-clipboard")
            {
                copyToClipboard = false;
                continue;
            }

            if (arg is "--env-probe" or "--probe-env")
            {
                envProbe = true;
                continue;
            }

            if (arg is "--extensions")
            {
                if (i + 1 >= args.Length)
                {
                    return null!;
                }

                extensionFilter = ParseExtensionFilter(args[++i]);
                if (extensionFilter is null || extensionFilter.Count == 0)
                {
                    return null!;
                }

                continue;
            }

            if (arg is "--max-file-bytes")
            {
                if (i + 1 >= args.Length || !long.TryParse(args[++i], out maxFileBytes) || maxFileBytes <= 0)
                {
                    return null!;
                }

                continue;
            }

            if (arg is "-o" or "--output")
            {
                if (i + 1 >= args.Length)
                {
                    return null!;
                }

                output = args[++i];
                continue;
            }

            if (arg.StartsWith('-'))
            {
                return null!;
            }

            if (arg.StartsWith('['))
            {
                var extensionArg = arg;
                while (!extensionArg.EndsWith(']'))
                {
                    if (i + 1 >= args.Length)
                    {
                        return null!;
                    }

                    extensionArg += $" {args[++i]}";
                }

                extensionFilter ??= [];
                extensionFilter = ParseExtensionFilter(extensionArg);
                if (extensionFilter is null || extensionFilter.Count == 0)
                {
                    return null!;
                }

                continue;
            }

            if (root == ".")
            {
                root = arg;
                continue;
            }

            return null!;
        }

        return new CliOptions(
            root,
            output,
            maxFileBytes,
            extensionFilter is null || extensionFilter.Count == 0 ? null : extensionFilter,
            quiet,
            verbose,
            listPlugins,
            mini,
            all,
            useGitignore,
            copyToClipboard,
            envProbe);
    }

    private static HashSet<string>? ParseExtensionFilter(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            trimmed = trimmed[1..^1];
        }

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.Length == 0)
            {
                continue;
            }

            var normalized = raw.StartsWith('.') ? raw : $".{raw}";
            extensions.Add(normalized.ToLowerInvariant());
        }

        return extensions.Count == 0 ? null : extensions;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
        codecat - concatenate source files into an LLM-friendly text container

        usage:
          codecat [root] [-o concat.txt] [options]

        options:
          -o, --output <path>       Output file. Default: concat.txt
          --extensions <list>        Limit files to extensions list, e.g. "[cs,csproj]"
          --max-file-bytes <bytes>  Skip files larger than this. Default: 250000
          --mini                    Use compact output and safe content minifiers
          --all                     Include broad optional source/docs files
          --use-gitignore           Exclude paths matched by .gitignore files
          --copy, --clipboard       Copy the output file to the system clipboard (default)
          --no-copy, --no-clipboard
                                    Do not copy the output file to the clipboard
          --env-probe               Print clipboard environment detection and exit
          --list-plugins            Print built-in plugin rules and exit
          -q, --quiet               Suppress progress output
          -v, --verbose             Print extra skip/progress details
          --version                 Show version
          -h, --help                Show help

        examples:
          codecat .
          codecat . -o context.txt
          codecat . --no-copy
          codecat --env-probe
          codecat D:\\Git\\my-app -o out\\concat.txt
          codecat . "[cs,csproj]"
          codecat . --extensions cs,csproj
        """);
    }

    public static void PrintPlugins(IReadOnlyList<ICodecatPlugin> plugins)
    {
        foreach (var plugin in plugins.OrderBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(plugin.Name);
            Console.WriteLine($"  include: {string.Join(", ", plugin.IncludeRules)}");
            Console.WriteLine($"  ignore_dirs: {string.Join(", ", plugin.IgnoreDirectoryRules.Take(20))}");
        }
    }

    public static string GetVersion()
    {
        return typeof(CliOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(CliOptions).Assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
