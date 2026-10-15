using Codecat.Cli;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Codecat.Tests;

public sealed class CliTests
{
    [Fact]
    public async Task NoCopyOptionDoesNotCallClipboardService()
    {
        using var temp = new TemporaryDirectory();
        var clipboard = new CountingClipboard();
        var runner = CreateRunner(clipboard);

        var exitCode = await runner.RunAsync([temp.Path, "--no-copy", "--quiet", "-o", Path.Combine(temp.Path, "result.txt")], CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, clipboard.Calls);
    }

    [Fact]
    public async Task EnabledCopyCallsClipboardOnceAfterWriting()
    {
        using var temp = new TemporaryDirectory();
        var clipboard = new CountingClipboard();
        var runner = CreateRunner(clipboard);

        var exitCode = await runner.RunAsync([temp.Path, "--quiet", "-o", Path.Combine(temp.Path, "result.txt")], CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(1, clipboard.Calls);
        Assert.True(clipboard.PathExistsWhenCalled);
    }

    [Fact]
    public async Task ZipModeCopiesTextContentsInsteadOfTheArchive()
    {
        using var temp = new TemporaryDirectory();
        var clipboard = new CountingClipboard();
        var runner = CreateRunner(clipboard);

        var exitCode = await runner.RunAsync([temp.Path, "--zip", "--quiet", "-o", Path.Combine(temp.Path, "context.txt")], CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(1, clipboard.Calls);
        Assert.Equal("written", clipboard.ContentWhenCalled);
        Assert.True(File.Exists(Path.Combine(temp.Path, "context.zip")));
    }

    private static CliRunner CreateRunner(CountingClipboard clipboard) => new(
        _ => new EmptyOrchestrator(), new TestWriter(), clipboard, Options.Create(new ScanConfiguration()),
        NullLogger<CliRunner>.Instance);

    private sealed class EmptyOrchestrator : IScanOrchestrator
    {
        public Task<ScanResult> ScanAsync(string root, string? outputPath, ScanOptions options, Action<ScanProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ScanResult([], [], new Dictionary<string, int>(), 1, 0, 0, 0, []));
    }

    private sealed class TestWriter : IScanResultWriter
    {
        public void Write(string root, string outputPath, ScanResult result, bool mini) => File.WriteAllText(outputPath, "written");
    }

    private sealed class CountingClipboard : IClipboardService
    {
        public int Calls { get; private set; }
        public bool PathExistsWhenCalled { get; private set; }
        public string? ContentWhenCalled { get; private set; }

        public ClipboardCopyResult CopyFile(string path)
        {
            Calls++;
            PathExistsWhenCalled = File.Exists(path);
            ContentWhenCalled = File.ReadAllText(path);
            return ClipboardCopyResult.Copied("test");
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codecat-cli-test-{Guid.NewGuid():N}");
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
