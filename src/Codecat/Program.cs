using Codecat.Cli;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;

if (args is ["--version"])
{
    Console.WriteLine(CliOptions.GetVersion());
    return 0;
}

if (args is ["-h"] or ["--help"])
{
    CliOptions.PrintUsage();
    return 0;
}

var options = CliOptions.Parse(args);
if (options is null)
{
    CliOptions.PrintUsage();
    return 1;
}

var root = Path.GetFullPath(options.RootPath);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"error: root directory does not exist: {root}");
    return 1;
}

var outputPath = Path.GetFullPath(options.OutputPath);
var plugins = PluginRegistry.CreateDefault();

if (options.ListPlugins)
{
    CliOptions.PrintPlugins(plugins);
    return 0;
}

if (!options.Quiet)
{
    Console.Error.WriteLine($"root: {root}");
    Console.Error.WriteLine($"output: {outputPath}");
    Console.Error.WriteLine($"plugins: {plugins.Count}");
}

try
{
    var scanOptions = new ScanOptions(options.MaxFileBytes, options.Quiet, options.Verbose);
    var scanner = new ProjectScanner(
        plugins,
        outputPath,
        scanOptions,
        progress => Console.Error.WriteLine($"scanning: dirs={progress.DirectoriesVisited} seen={progress.FilesSeen} included={progress.FilesIncluded} skipped={progress.ItemsSkipped} current=\"{progress.CurrentPath}\""));
    var result = scanner.Scan(root);

    var writer = new CodecatWriter();
    writer.Write(root, outputPath, result);

    Console.WriteLine($"wrote {result.FilesIncluded} files to {outputPath}");
    if (!options.Quiet)
    {
        Console.Error.WriteLine($"done: dirs={result.DirectoriesVisited} seen={result.FilesSeen} included={result.FilesIncluded} skipped={result.ItemsSkipped} warnings={result.Warnings.Count}");
    }

    return 0;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
{
    Console.Error.WriteLine($"error: {exception.Message}");
    return 1;
}
