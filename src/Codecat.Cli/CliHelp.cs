using System.Reflection;
using Codecat.Plugins;
using Codecat.Scanning;

namespace Codecat.Cli;

public static class CliHelp
{
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
          --docs                    Include Markdown .md files (also enabled by --all)
          --lines                   Print physical line counts for all recognized plugins to stdout
          --tree, -tree             Add folder totals to --lines
          --depth <N>               Show folders through depth N (root is 0); requires --tree
          --use-gitignore           Exclude paths matched by .gitignore files
          --zip                     Write a maximum-compression ZIP archive
          -xz, --xz                Write XZ-compressed Base64 text (ASCII)
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
          codecat . --zip
          codecat . -xz
          codecat . --no-copy
          codecat --env-probe
          codecat D:\\Git\\my-app -o out\\concat.txt
          codecat . "[cs,csproj]"
          codecat . --extensions cs,csproj
          codecat . --lines
          codecat . --lines --tree --depth 2 --docs

        Recognized source includes .rs, .wgsl, .ron, .toml and .ftl. Markdown .md
        requires --docs or --all. --lines counts blank lines and comments, has no
        default size limit, and ends with TOTAL: <files> files, <lines> lines.
        """);
    }

    public static void PrintPlugins(IReadOnlyList<ICodecatPlugin> plugins)
    {
        foreach (var plugin in plugins.OrderBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(plugin.Name);
            Console.WriteLine($"  include: {string.Join(", ", plugin.IncludeRules)}");
        }
        Console.WriteLine($"global ignore_dirs: {string.Join(", ", GlobalDirectoryRules.All.Take(20))}");
    }

    public static string GetVersion()
    {
        return typeof(CliHelp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(CliHelp).Assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
