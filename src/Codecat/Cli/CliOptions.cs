using Codecat.Plugins;
using System.Reflection;

namespace Codecat.Cli;

internal sealed record CliOptions(
    string RootPath,
    string OutputPath,
    long MaxFileBytes,
    bool Quiet,
    bool Verbose,
    bool ListPlugins)
{
    public const long DefaultMaxFileBytes = 250_000;

    public static CliOptions? Parse(string[] args)
    {
        var root = ".";
        var output = "concat.txt";
        var maxFileBytes = DefaultMaxFileBytes;
        var quiet = false;
        var verbose = false;
        var listPlugins = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                return null;
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

            if (arg is "--max-file-bytes")
            {
                if (i + 1 >= args.Length || !long.TryParse(args[++i], out maxFileBytes) || maxFileBytes <= 0)
                {
                    return null;
                }

                continue;
            }

            if (arg is "-o" or "--output")
            {
                if (i + 1 >= args.Length)
                {
                    return null;
                }

                output = args[++i];
                continue;
            }

            if (arg.StartsWith('-'))
            {
                return null;
            }

            root = arg;
        }

        return new CliOptions(root, output, maxFileBytes, quiet, verbose, listPlugins);
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
        codecat - concatenate source files into an LLM-friendly text container

        usage:
          codecat [root] [-o concat.txt] [options]

        options:
          -o, --output <path>       Output file. Default: concat.txt
          --max-file-bytes <bytes>  Skip files larger than this. Default: 250000
          --list-plugins           Print built-in plugin rules and exit
          -q, --quiet              Suppress progress output
          -v, --verbose            Print extra skip/progress details
          --version                Show version
          -h, --help               Show help

        examples:
          codecat .
          codecat . -o context.txt
          codecat D:\Git\my-app -o out\concat.txt
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
