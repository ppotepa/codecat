using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Codecat.Cli;

public sealed class CliRunner(
    Func<bool, IScanOrchestrator> orchestratorFactory,
    IScanResultWriter writer,
    IOptions<ScanConfiguration> configuration,
    ILogger<CliRunner> logger)
{
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
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

        var options = CliOptions.Parse(args, configuration.Value);
        if (options is null)
        {
            CliOptions.PrintUsage();
            return 1;
        }

        if (options.EnvProbe)
        {
            var environment = ClipboardEnvironmentProbe.Capture();
            ClipboardEnvironmentProbe.Print(environment, ClipboardCopier.GetStrategyNames(environment), Console.Out);
            return 0;
        }

        var defaults = configuration.Value;
        var includeAllPlugins = defaults.DefaultAllPlugins || options.All;
        var plugins = new PluginRegistry(includeAllPlugins).GetPlugins();
        if (options.ListPlugins)
        {
            CliOptions.PrintPlugins(plugins);
            return 0;
        }

        var root = Path.GetFullPath(options.RootPath);
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"error: root directory does not exist: {root}");
            return 1;
        }

        var requestedOutputPath = Path.GetFullPath(options.OutputPath);
        var outputPath = options.ZipOutput
            ? ResolveZipArchivePath(requestedOutputPath)
            : requestedOutputPath;
        var zipEntryName = options.ZipOutput
            ? ResolveZipEntryName(requestedOutputPath)
            : string.Empty;

        if (!options.Quiet)
        {
            Console.Error.WriteLine($"root: {root}");
            Console.Error.WriteLine($"output: {outputPath}");
            Console.Error.WriteLine($"plugins: {plugins.Count}");
            Console.Error.WriteLine($"mode: {(options.All ? "all" : "default")}");
            if (options.UseGitignore)
            {
                Console.Error.WriteLine("gitignore: enabled");
            }

            if (options.ZipOutput)
            {
                Console.Error.WriteLine($"zip: enabled ({zipEntryName})");
            }
        }

        try
        {
            string? tempOutputPath = null;
            var scanOptions = new ScanOptions(
                options.MaxFileBytes,
                options.Quiet,
                options.Verbose,
                options.Mini,
                options.UseGitignore,
                options.ExtensionFilter);

            var orchestrator = orchestratorFactory(options.All);
            var result = await orchestrator.ScanAsync(root, outputPath, scanOptions, progress =>
            {
                logger.LogInformation("scanning: dirs={Directories} seen={Seen} included={Included} skipped={Skipped} current={Current}", progress.DirectoriesVisited, progress.FilesSeen, progress.FilesIncluded, progress.ItemsSkipped, progress.CurrentPath);
            }, cancellationToken);

            try
            {
                var textOutputPath = outputPath;
                if (options.ZipOutput)
                {
                    tempOutputPath = Path.Combine(Path.GetTempPath(), $"codecat-{Guid.NewGuid():N}-{zipEntryName}");
                    textOutputPath = tempOutputPath;
                }

                writer.Write(root, textOutputPath, result, options.Mini);

                if (options.ZipOutput)
                {
                    ZipArchiveWriter.WriteSingleFileArchive(textOutputPath, outputPath, zipEntryName);
                }
            }
            finally
            {
                TryDeleteTempOutput(tempOutputPath);
            }

            Console.WriteLine($"wrote {result.FilesIncluded} files to {outputPath}");
            if (options.CopyToClipboard)
            {
                var copyResult = ClipboardCopier.CopyFile(outputPath);
                if (!copyResult.Success)
                {
                    Console.Error.WriteLine($"warning: {copyResult.Message}");
                }
                else
                {
                    Console.WriteLine(copyResult.Message);
                }
            }

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
    }

    private static string ResolveZipArchivePath(string requestedOutputPath)
    {
        return string.Equals(Path.GetExtension(requestedOutputPath), ".zip", StringComparison.OrdinalIgnoreCase)
            ? requestedOutputPath
            : Path.ChangeExtension(requestedOutputPath, ".zip");
    }

    private static string ResolveZipEntryName(string requestedOutputPath)
    {
        var fileName = Path.GetFileName(requestedOutputPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "concat.txt";
        }

        return string.Equals(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase)
            ? Path.ChangeExtension(fileName, ".txt")
            : fileName;
    }

    private static void TryDeleteTempOutput(string? tempOutputPath)
    {
        if (string.IsNullOrWhiteSpace(tempOutputPath))
        {
            return;
        }

        try
        {
            File.Delete(tempOutputPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
        }
    }
}
