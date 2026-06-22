using System.Diagnostics;
using System.Text;

namespace Codecat.Output;

internal static class ClipboardCopier
{
    public static ClipboardCopyResult CopyFile(string path)
    {
        if (!File.Exists(path))
        {
            return ClipboardCopyResult.Failed($"output file does not exist: {path}");
        }

        var environment = ClipboardEnvironmentProbe.Capture();
        var candidates = GetCandidates(environment);
        var attempted = new List<string>();
        var failures = new List<string>();

        foreach (var candidate in candidates)
        {
            attempted.Add(candidate.Name);
            var result = candidate.Kind switch
            {
                ClipboardStrategyKind.Process => TryCopyWithProcess(candidate, path),
                ClipboardStrategyKind.Osc52 => TryCopyWithOsc52(path, environment),
                _ => ClipboardCopyResult.Failed($"unsupported clipboard strategy: {candidate.Kind}")
            };

            if (result.Success)
            {
                return result;
            }

            failures.Add(result.Message);
        }

        if (environment.IsLinux)
        {
            return ClipboardCopyResult.Failed(
                "could not copy to clipboard; for SSH/tmux use a terminal with OSC 52 enabled, or install one of: wl-clipboard (wl-copy), xclip, xsel"
                + FormatFailures(failures));
        }

        return ClipboardCopyResult.Failed(
            $"could not copy to clipboard; tried: {string.Join(", ", attempted)}{FormatFailures(failures)}");
    }

    public static IReadOnlyList<string> GetStrategyNames(ClipboardEnvironment environment)
    {
        return GetCandidates(environment).Select(candidate => candidate.Name).ToArray();
    }

    private static IReadOnlyList<ClipboardCommand> GetCandidates(ClipboardEnvironment environment)
    {
        var candidates = new List<ClipboardCommand>();
        if (environment.IsTmux)
        {
            candidates.Add(ClipboardCommand.Process("tmux load-buffer -w", "tmux", "load-buffer", "-w", "-"));
        }

        if (environment.IsSsh && environment.HasConsole)
        {
            candidates.Add(ClipboardCommand.Osc52());
        }

        if (environment.IsWindows)
        {
            candidates.Add(ClipboardCommand.Process("clip.exe", "clip.exe"));
            return candidates;
        }

        if (environment.IsMacOS)
        {
            candidates.Add(ClipboardCommand.Process("pbcopy", "pbcopy"));
            AddTerminalOsc52Fallback(candidates, environment);
            return candidates;
        }

        if (environment.IsLinux)
        {
            if (environment.HasWayland)
            {
                candidates.Add(ClipboardCommand.Process("wl-copy", "wl-copy"));
            }

            candidates.Add(ClipboardCommand.Process("xclip", "xclip", "-selection", "clipboard"));
            candidates.Add(ClipboardCommand.Process("xsel", "xsel", "--clipboard", "--input"));

            if (!environment.HasWayland)
            {
                candidates.Add(ClipboardCommand.Process("wl-copy", "wl-copy"));
            }

            AddTerminalOsc52Fallback(candidates, environment);
            return candidates;
        }

        AddTerminalOsc52Fallback(candidates, environment);
        return candidates;
    }

    private static void AddTerminalOsc52Fallback(List<ClipboardCommand> candidates, ClipboardEnvironment environment)
    {
        if (!environment.IsSsh && environment.HasConsole)
        {
            candidates.Add(ClipboardCommand.Osc52());
        }
    }

    private static ClipboardCopyResult TryCopyWithProcess(ClipboardCommand command, string path)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command.Command,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            foreach (var argument in command.Arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return ClipboardCopyResult.Failed($"{command.Name} did not start");
            }

            var errorTask = process.StandardError.ReadToEndAsync();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            using (var input = process.StandardInput.BaseStream)
            using (var file = File.OpenRead(path))
            {
                file.CopyTo(input);
            }

            process.WaitForExit();
            _ = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult().Trim();
            if (process.ExitCode == 0)
            {
                return ClipboardCopyResult.Copied(command.Name);
            }

            return ClipboardCopyResult.Failed(
                string.IsNullOrWhiteSpace(error)
                    ? $"{command.Name} exited with code {process.ExitCode}"
                    : $"{command.Name}: {error}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return ClipboardCopyResult.Failed($"{command.Name}: {exception.Message}");
        }
    }

    private static ClipboardCopyResult TryCopyWithOsc52(string path, ClipboardEnvironment environment)
    {
        if (!environment.HasConsole)
        {
            return ClipboardCopyResult.Failed("OSC 52: no interactive terminal is available");
        }

        try
        {
            var payload = Convert.ToBase64String(File.ReadAllBytes(path));
            var sequence = $"\u001b]52;c;{payload}\a";
            var bytes = Encoding.ASCII.GetBytes(sequence);

            if (environment.CanWriteDevTty)
            {
                using var tty = File.OpenWrite("/dev/tty");
                tty.Write(bytes);
                tty.Flush();
            }
            else
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(bytes);
                stdout.Flush();
            }

            return ClipboardCopyResult.Copied("terminal OSC 52");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return ClipboardCopyResult.Failed($"terminal OSC 52: {exception.Message}");
        }
    }

    private static string FormatFailures(IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
        {
            return string.Empty;
        }

        return $". Last error: {failures[^1]}";
    }

    private sealed record ClipboardCommand(
        ClipboardStrategyKind Kind,
        string Name,
        string Command,
        IReadOnlyList<string> Arguments)
    {
        public static ClipboardCommand Process(string name, string command, params string[] arguments)
        {
            return new ClipboardCommand(ClipboardStrategyKind.Process, name, command, arguments);
        }

        public static ClipboardCommand Osc52()
        {
            return new ClipboardCommand(ClipboardStrategyKind.Osc52, "terminal OSC 52", string.Empty, []);
        }
    }

    private enum ClipboardStrategyKind
    {
        Process,
        Osc52
    }
}

internal sealed record ClipboardCopyResult(bool Success, string Message)
{
    public static ClipboardCopyResult Copied(string tool) => new(true, $"copied output to clipboard using {tool}");

    public static ClipboardCopyResult Failed(string message) => new(false, message);
}
