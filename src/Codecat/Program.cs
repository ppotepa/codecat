using Codecat.Cli;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;

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
var scanner = new ProjectScanner(plugins, outputPath);
var files = scanner.Scan(root);

var writer = new CodecatWriter();
writer.Write(root, outputPath, files);

Console.WriteLine($"wrote {files.Count} files to {outputPath}");
return 0;
