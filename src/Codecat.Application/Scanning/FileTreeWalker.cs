using Microsoft.Extensions.Logging;
using Codecat.Plugins;
using Codecat.Plugins.Matching;

namespace Codecat.Scanning;

public sealed class FileTreeWalker(
    IPluginRegistry pluginRegistry,
    FileProcessor fileProcessor,
    IErrorHandler<ScanError> errorHandler,
    ILogger<FileTreeWalker> logger)
{
    private readonly IReadOnlyList<ICodecatPlugin> _plugins = pluginRegistry.GetPlugins();

    internal async Task WalkAsync(
        string root,
        string? outputPath,
        ScanOptions options,
        ScanSession session,
        Action<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var rootFullPath = Path.GetFullPath(root);
        var rootInfo = new DirectoryInfo(rootFullPath);
        if (rootInfo.LinkTarget is not null)
        {
            rootFullPath = rootInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? rootFullPath;
        }
        outputPath = outputPath is null ? null : Path.GetFullPath(outputPath);
        var gitignoreRules = options.UseGitignore
            ? LoadGitignoreRules(session, rootFullPath, rootFullPath, GitignoreRules.Empty)
            : GitignoreRules.Empty;

        await WalkDirectoryAsync(session, rootFullPath, rootFullPath, gitignoreRules, options, progress, outputPath, cancellationToken);
    }

    private async Task WalkDirectoryAsync(
        ScanSession session,
        string root,
        string currentDirectory,
        GitignoreRules gitignoreRules,
        ScanOptions options,
        Action<ScanProgress>? progress,
        string? outputPath,
        CancellationToken cancellationToken)
    {
        session.DirectoriesVisited++;
        ReportProgress(session, progress, root, currentDirectory, options);

        foreach (var directory in SafeEnumerateDirectories(session, root, currentDirectory))
        {
            var relative = ToRelativePath(root, directory);
            bool directoryIsLink;
            try { directoryIsLink = IsReparsePoint(directory); }
            catch (Exception ex) when (IsFileAccessException(ex))
            {
                RecordFileError(session, relative, ex, ScanErrorKind.DirectoryEnumeration);
                continue;
            }
            if (directoryIsLink)
            {
                Skip(session, "symbolic_link");
                ReportVerbose($"skip dir: {relative} (symbolic_link)", options);
                continue;
            }

            var globalDenyReason = GlobalScanRules.TryDenyDirectory(relative);
            if (globalDenyReason is not null)
            {
                Skip(session, globalDenyReason);
                ReportVerbose($"skip dir: {relative} ({globalDenyReason})", options);
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: true))
            {
                Skip(session, "gitignore");
                ReportVerbose($"skip dir: {relative} (gitignore)", options);
                continue;
            }

            var childGitignoreRules = options.UseGitignore
                ? LoadGitignoreRules(session, root, directory, gitignoreRules)
                : gitignoreRules;

            await WalkDirectoryAsync(session, root, directory, childGitignoreRules, options, progress, outputPath, cancellationToken);
        }

        foreach (var file in SafeEnumerateFiles(session, root, currentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            session.FilesSeen++;
            var fullPath = Path.GetFullPath(file);
            var relative = ToRelativePath(root, fullPath);

            bool fileIsLink;
            try { fileIsLink = IsReparsePoint(fullPath); }
            catch (Exception ex) when (IsFileAccessException(ex))
            {
                RecordFileError(session, relative, ex, ScanErrorKind.FileEnumeration);
                continue;
            }
            if (fileIsLink)
            {
                Skip(session, "symbolic_link");
                ReportVerbose($"skip file: {relative} (symbolic_link)", options);
                continue;
            }

            var globalDenyReason = GlobalScanRules.TryDenyFileBeforePluginMatch(fullPath, relative, outputPath);
            if (globalDenyReason is not null)
            {
                Skip(session, globalDenyReason);
                ReportVerbose($"skip file: {relative} ({globalDenyReason})", options);
                continue;
            }

            if (options.UseGitignore && gitignoreRules.IsIgnored(relative, isDirectory: false))
            {
                Skip(session, "gitignore");
                ReportVerbose($"skip file: {relative} (gitignore)", options);
                continue;
            }

            if (options.ExtensionFilter is not null && options.ExtensionFilter.Count > 0)
            {
                if (!options.ExtensionFilter.Any(extension => relative.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    Skip(session, "extension_filter");
                    ReportVerbose($"skip file: {relative} (extension_filter)", options);
                    continue;
                }
            }

            var pluginMatch = PluginMatcher.Match(relative, _plugins);
            if (pluginMatch is null)
            {
                Skip(session, "no_plugin_match");
                ReportVerbose($"skip file: {relative} (no plugin match)", options);
                continue;
            }

            await TryReadAndProcessFileAsync(session, fullPath, relative, pluginMatch, options, cancellationToken);
            ReportProgress(session, progress, root, relative, options);
        }
    }

    private async Task TryReadAndProcessFileAsync(
        ScanSession session,
        string fullPath,
        string relative,
        PluginMatch pluginMatch,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        var result = await fileProcessor.ProcessAsync(fullPath, relative, pluginMatch, options, cancellationToken);
        if (result.SkipReason is not null)
        {
            Skip(session, result.SkipReason);
            ReportVerbose($"skip file: {relative} ({result.SkipReason})", options);
            return;
        }

        if (result.File is not null)
        {
            session.Files.Add(result.File);
            session.FilesIncluded++;
        }
        else if (result.LineCount is not null)
        {
            session.LineCounts.Add(result.LineCount);
            session.FilesIncluded++;
        }
        else if (result.Error is not null)
        {
            Skip(session, "read_error");
            session.Warnings.Add(new ScanWarning(relative, result.Error.Message));
            errorHandler.Handle(result.Error, relative);
        }
    }

    private GitignoreRules LoadGitignoreRules(ScanSession session, string root, string currentDirectory, GitignoreRules rules)
    {
        var gitignorePath = Path.Combine(currentDirectory, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return rules;
        }

        try
        {
            var baseRelativePath = ToRelativePath(root, currentDirectory);
            var updated = rules.AddFromFile(gitignorePath, baseRelativePath);
            var relativeGitignorePath = ToRelativePath(root, gitignorePath);
            foreach (var warning in updated.ParseWarnings.Skip(rules.ParseWarnings.Count))
            {
                session.Warnings.Add(new ScanWarning(relativeGitignorePath, warning));
                errorHandler.Handle(new ScanError(ScanErrorKind.Gitignore, relativeGitignorePath, warning), gitignorePath);
            }
            return updated;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            session.Warnings.Add(new ScanWarning(ToRelativePath(root, gitignorePath), $"could not read .gitignore: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.Gitignore, ToRelativePath(root, gitignorePath), exception.Message), gitignorePath);
            return rules;
        }
    }

    private IEnumerable<string> SafeEnumerateDirectories(ScanSession session, string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateDirectories(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip(session, "directory_error");
            session.Warnings.Add(new ScanWarning(ToRelativePath(root, currentDirectory), $"could not enumerate directories: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.DirectoryEnumeration, ToRelativePath(root, currentDirectory), exception.Message), currentDirectory);
            logger.LogWarning("could not enumerate directories {Directory}", currentDirectory);
            return [];
        }
    }

    private IEnumerable<string> SafeEnumerateFiles(ScanSession session, string root, string currentDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(currentDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip(session, "directory_error");
            session.Warnings.Add(new ScanWarning(ToRelativePath(root, currentDirectory), $"could not enumerate files: {exception.Message}"));
            errorHandler.Handle(new ScanError(ScanErrorKind.FileEnumeration, ToRelativePath(root, currentDirectory), exception.Message), currentDirectory);
            logger.LogWarning("could not enumerate files {Directory}", currentDirectory);
            return [];
        }
    }

    private static void Skip(ScanSession session, string reason)
    {
        session.ItemsSkipped++;
        session.SkippedByReason[reason] = session.SkippedByReason.GetValueOrDefault(reason) + 1;
    }

    private static void ReportProgress(ScanSession session, Action<ScanProgress>? progress, string root, string currentPath, ScanOptions options)
    {
        if (progress is null || options.Quiet || session.FilesSeen == 0 || session.FilesSeen % 100 != 0)
        {
            return;
        }

        progress(new ScanProgress(session.DirectoriesVisited, session.FilesSeen, session.FilesIncluded, session.ItemsSkipped, ToRelativePath(root, currentPath)));
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

    private void RecordFileError(ScanSession session, string relative, Exception exception, ScanErrorKind kind)
    {
        Skip(session, "read_error");
        session.Warnings.Add(new ScanWarning(relative, exception.Message));
        errorHandler.Handle(new ScanError(kind, relative, exception.Message), relative);
        logger.LogWarning(exception, "could not process file {Path}", relative);
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsFileAccessException(Exception exception) => exception is
        IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException;

}
