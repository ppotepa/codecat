using Microsoft.Extensions.Logging;
using Codecat.Minification;
using Codecat.Plugins;

namespace Codecat.Scanning;

public sealed class ScanOrchestrator(
    IPluginRegistry pluginRegistry,
    IFileReader fileReader,
    IMinifierRegistry minifiers,
    IMetricsCalculator metricsCalculator,
    IErrorHandler<ScanError> errorHandler,
    ILogger<ScanOrchestrator> logger) : IScanOrchestrator
{
    private readonly IReadOnlyList<ICodecatPlugin> _plugins = pluginRegistry.GetPlugins();
    private readonly Dictionary<string, int> _skippedByReason = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ScanWarning> _warnings = [];
    private readonly List<CodecatFile> _files = [];
    private int _directoriesVisited;
    private int _filesSeen;
    private int _filesIncluded;
    private int _itemsSkipped;

    public async Task<ScanResult> ScanAsync(
        string root,
        string outputPath,
        ScanOptions options,
        Action<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rootFullPath = Path.GetFullPath(root);
        outputPath = Path.GetFullPath(outputPath);
        var gitignoreRules = options.UseGitignore
            ? LoadGitignoreRules(rootFullPath, rootFullPath, GitignoreRules.Empty)
            : GitignoreRules.Empty;

        await WalkAsync(rootFullPath, rootFullPath, gitignoreRules, options, progress, outputPath, cancellationToken);

        _files.Sort(static (left, right) => string.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase));
        return new ScanResult(
            _files.ToArray(),
            _warnings.ToArray(),
            _skippedByReason.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            _directoriesVisited,
            _filesSeen,
            _filesIncluded,
            _itemsSkipped);
    }

    private async Task WalkAsync(
        string root,
        string currentDirectory,
        GitignoreRules gitignoreRules,
        ScanOptions options,
        Action<ScanProgress>? progress,
        string outputPath,
        CancellationToken cancellationToken)
    {
        _directoriesVisited++;
        ReportProgress(progress, root, currentDirectory, options);

        foreach (var directory in SafeEnumerateDirectories(root, currentDirectory))
        {
            var relative = ToRelativePath(root, directory);
            var globalDenyReason = GlobalScanRules.TryDenyDirectory(relative);
            if (globalDenyReason is not null)
            {
                Skip("hidden_directory");
                ReportVerbose($"skip dir: {relative} ({globalDenyReason})", options);
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: true))
            {
                Skip("gitignore");
                ReportVerbose($"skip dir: {relative} (gitignore)", options);
                continue;
            }

            if (_plugins.Any(plugin => plugin.ShouldIgnoreDirectory(relative)))
            {
                Skip("ignored_directory");
                ReportVerbose($"skip dir: {relative}", options);
                continue;
            }

            var childGitignoreRules = options.UseGitignore
                ? LoadGitignoreRules(root, directory, gitignoreRules)
                : gitignoreRules;

            await WalkAsync(root, directory, childGitignoreRules, options, progress, outputPath, cancellationToken);
        }

        foreach (var file in SafeEnumerateFiles(root, currentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            _filesSeen++;
            var fullPath = Path.GetFullPath(file);
            var relative = ToRelativePath(root, fullPath);

            var globalDenyReason = GlobalScanRules.TryDenyFileBeforePluginMatch(fullPath, relative, outputPath);
            if (globalDenyReason is not null)
            {
                Skip(globalDenyReason);
                ReportVerbose($"skip file: {relative} ({globalDenyReason})", options);
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: false))
            {
                Skip("gitignore");
                ReportVerbose($"skip file: {relative} (gitignore)", options);
                continue;
            }

            if (options.ExtensionFilter is not null && options.ExtensionFilter.Count > 0)
            {
                var extension = Path.GetExtension(relative);
                if (!options.ExtensionFilter.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    Skip("extension_filter");
                    ReportVerbose($"skip file: {relative} (extension_filter)", options);
                    continue;
                }
            }

            var pluginMatch = _plugins
                .Select(plugin => plugin.TryMatchFile(relative))
                .FirstOrDefault(match => match is not null);
            if (pluginMatch is null)
            {
                Skip("no_plugin_match");
                ReportVerbose($"skip file: {relative} (no plugin match)", options);
                continue;
            }

            await TryReadAndProcessFileAsync(fullPath, relative, pluginMatch, options, cancellationToken);
            ReportProgress(progress, root, relative, options);
        }
    }

    private async Task TryReadAndProcessFileAsync(
        string fullPath,
        string relative,
        PluginMatch pluginMatch,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(fullPath);
        var safetyDenyReason = GlobalScanRules.TryDenyFileAfterPluginMatch(fullPath, info, options.MaxFileBytes);
        if (safetyDenyReason is not null)
        {
            Skip(safetyDenyReason);
            ReportVerbose($"skip file: {relative} ({safetyDenyReason})", options);
            return;
        }

        var content = await fileReader.ReadAsync(fullPath, relative, cancellationToken);
        if (!content.IsSuccess)
        {
            Skip("read_error");
            var message = content.Errors.IsDefaultOrEmpty ? "unknown read error" : content.Errors[0].Message;
            _warnings.Add(new ScanWarning(relative, message));
            errorHandler.Handle(content.Errors.First(), relative);
            logger.LogWarning("could not read file {Path}: {Message}", relative, message);
            return;
        }

        var originalContent = content.Value!.Content;
        if (options.MaxFileBytes > 0 && info.Length > options.MaxFileBytes)
        {
            Skip("too_large");
            ReportVerbose($"skip file: {relative} (too_large)", options);
            return;
        }

        try
        {
            var processed = originalContent;
            var minified = false;
            if (options.Mini && minifiers.TryMinify(originalContent, pluginMatch.Language, out var minifiedContent))
            {
                processed = minifiedContent;
                minified = true;
            }

            var metrics = metricsCalculator.Calculate(fullPath, processed, originalContent);
            _files.Add(new CodecatFile(
                RelativePath: relative,
                Plugin: pluginMatch.PluginName,
                Language: pluginMatch.Language,
                Reason: pluginMatch.Reason,
                Bytes: metrics.Bytes,
                Lines: metrics.Lines,
                OriginalBytes: metrics.OriginalBytes,
                OriginalLines: metrics.OriginalLines,
                Minified: minified,
                Sha256: metrics.Sha256,
                Content: processed));
            _filesIncluded++;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Skip("read_error");
            _warnings.Add(new ScanWarning(relative, ex.Message));
            errorHandler.Handle(new ScanError(ScanErrorKind.MetricsCalculation, relative, ex.Message), relative);
            logger.LogWarning(ex, "processing failed: {Path}", relative);
        }
    }

    private GitignoreRules LoadGitignoreRules(string root, string currentDirectory, GitignoreRules rules)
    {
        var gitignorePath = Path.Combine(currentDirectory, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return rules;
        }

        try
        {
            var baseRelativePath = ToRelativePath(root, currentDirectory);
            return rules.AddFromFile(gitignorePath, baseRelativePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            _warnings.Add(new ScanWarning(ToRelativePath(root, gitignorePath), $"could not read .gitignore: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.Gitignore, ToRelativePath(root, gitignorePath), exception.Message), gitignorePath);
            return rules;
        }
    }

    private IEnumerable<string> SafeEnumerateDirectories(string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateDirectories(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip("directory_error");
            _warnings.Add(new ScanWarning(ToRelativePath(root, currentDirectory), $"could not enumerate directories: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.DirectoryEnumeration, ToRelativePath(root, currentDirectory), exception.Message), currentDirectory);
            logger.LogWarning("could not enumerate directories {Directory}", currentDirectory);
            return [];
        }
    }

    private IEnumerable<string> SafeEnumerateFiles(string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip("directory_error");
            _warnings.Add(new ScanWarning(ToRelativePath(root, currentDirectory), $"could not enumerate files: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.FileEnumeration, ToRelativePath(root, currentDirectory), exception.Message), currentDirectory);
            logger.LogWarning("could not enumerate files {Directory}", currentDirectory);
            return [];
        }
    }

    private void Skip(string reason)
    {
        _itemsSkipped++;
        _skippedByReason[reason] = _skippedByReason.GetValueOrDefault(reason) + 1;
    }

    private void ReportProgress(Action<ScanProgress>? progress, string root, string currentPath, ScanOptions options)
    {
        if (progress is null || options.Quiet || _filesSeen == 0 || _filesSeen % 100 != 0)
        {
            return;
        }

        progress(new ScanProgress(_directoriesVisited, _filesSeen, _filesIncluded, _itemsSkipped, ToRelativePath(root, currentPath)));
    }

    private void ReportVerbose(string message, ScanOptions options)
    {
        if (options.Verbose && !options.Quiet)
        {
            logger.LogInformation(message);
        }
    }

    private static string ToRelativePath(string root, string fullPath)
    {
        var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return relative == "." ? "." : relative;
    }
}
