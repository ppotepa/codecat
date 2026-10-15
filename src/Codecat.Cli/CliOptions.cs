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
    bool ZipOutput,
    bool XzOutput,
    bool CopyToClipboard,
    bool EnvProbe,
    bool Docs,
    bool Lines,
    bool Tree,
    int? Depth,
    bool MaxFileBytesExplicit)
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
        var zipOutput = false;
        var xzOutput = false;
        var copyToClipboard = true;
        var envProbe = false;
        var docs = false;
        var lines = false;
        var tree = false;
        int? depth = null;
        var maxFileBytesExplicit = false;
        var outputOptionUsed = false;
        var copyOptionUsed = false;

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

            if (arg is "--docs") { docs = true; continue; }
            if (arg is "--lines") { lines = true; continue; }
            if (arg is "--tree" or "-tree") { tree = true; continue; }
            if (arg is "--depth")
            {
                if (i + 1 >= args.Length || !int.TryParse(args[++i], out var parsedDepth) || parsedDepth < 0) return null!;
                depth = parsedDepth;
                continue;
            }

            if (arg is "--use-gitignore" or "--gitignore")
            {
                useGitignore = true;
                continue;
            }

            if (arg is "--zip")
            {
                zipOutput = true;
                continue;
            }

            if (arg is "-xz" or "--xz")
            {
                xzOutput = true;
                continue;
            }

            if (arg is "--copy" or "--clipboard")
            {
                copyOptionUsed = true;
                copyToClipboard = true;
                continue;
            }

            if (arg is "--no-copy" or "--no-clipboard")
            {
                copyOptionUsed = true;
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

                maxFileBytesExplicit = true;

                continue;
            }

            if (arg is "-o" or "--output")
            {
                outputOptionUsed = true;
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

        if ((tree && !lines) || (depth is not null && !tree) || (zipOutput && xzOutput) ||
            (lines && (outputOptionUsed || zipOutput || xzOutput || mini || copyOptionUsed))) return null!;

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
            zipOutput,
            xzOutput,
            copyToClipboard,
            envProbe,
            docs,
            lines,
            tree,
            depth,
            maxFileBytesExplicit);
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


}
