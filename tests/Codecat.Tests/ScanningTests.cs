using System.Security.Cryptography;
using System.Text;
using Codecat.Minification;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Codecat.Tests;

public sealed class ScanningTests
{
    [Fact]
    public void UnsafeMinifiersPreserveSourceVerbatim()
    {
        const string css = "p::before { content: \"a  b /*keep*/\"; width: calc(100% + 2px); }";
        const string xml = "<p><b>Hello</b> <i>world</i></p>";

        Assert.False(new CssMinifier().TryMinify(css, out var cssResult));
        Assert.Equal(css, cssResult);
        Assert.False(new XmlMinifier().TryMinify(xml, out var xmlResult));
        Assert.Equal(xml, xmlResult);
    }

    [Fact]
    public async Task ReusingOrchestratorStartsANewScanSession()
    {
        using var temp = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "one.cs"), "class One {}\n");
        var scanner = CreateScanner();

        var first = await scanner.ScanAsync(temp.Path, null, Options());
        var second = await scanner.ScanAsync(temp.Path, null, Options());

        Assert.Single(first.Files);
        Assert.Single(second.Files);
        Assert.Equal(1, second.FilesSeen);
    }

    [Fact]
    public async Task ReadFailureDoesNotStopOtherFiles()
    {
        using var temp = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "a-bad.cs"), "bad\n");
        File.WriteAllText(Path.Combine(temp.Path, "b-good.cs"), "good\n");
        var scanner = CreateScanner(new FailingFileReader("a-bad.cs"));

        var result = await scanner.ScanAsync(temp.Path, null, Options());

        Assert.Single(result.Files);
        Assert.Equal("b-good.cs", result.Files[0].RelativePath);
        Assert.Single(result.Warnings);
        Assert.Equal(1, result.SkippedByReason["read_error"]);
    }

    [Fact]
    public async Task SymlinkedFilesAreSkipped()
    {
        if (OperatingSystem.IsWindows()) return;
        using var temp = new TemporaryDirectory();
        var outside = Path.Combine(Path.GetTempPath(), $"codecat-outside-{Guid.NewGuid():N}.cs");
        File.WriteAllText(outside, "outside\n");
        try
        {
            File.CreateSymbolicLink(Path.Combine(temp.Path, "linked.cs"), outside);
            var result = await CreateScanner().ScanAsync(temp.Path, null, Options());
            Assert.Empty(result.Files);
            Assert.Equal(1, result.SkippedByReason["symbolic_link"]);
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task SymlinkedDirectoriesAreSkipped()
    {
        if (OperatingSystem.IsWindows()) return;
        using var temp = new TemporaryDirectory();
        var outside = Path.Combine(Path.GetTempPath(), $"codecat-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "hidden.cs"), "outside\n");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(temp.Path, "linked"), outside);
            var result = await CreateScanner().ScanAsync(temp.Path, null, Options());
            Assert.Empty(result.Files);
            Assert.Equal(1, result.SkippedByReason["symbolic_link"]);
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidGitignoreRuleWarnsAndScanContinues()
    {
        using var temp = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ".gitignore"), "[z-a]\n");
        File.WriteAllText(Path.Combine(temp.Path, "valid.cs"), "valid\n");

        var result = await CreateScanner().ScanAsync(temp.Path, null, Options(useGitignore: true));

        Assert.Contains(result.Files, file => file.RelativePath == "valid.cs");
        Assert.Contains(result.Warnings, warning => warning.Path == ".gitignore" && warning.Message.Contains("line 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Utf16BomIsDecodedAndHashedFromOriginalBytes()
    {
        using var temp = new TemporaryDirectory();
        const string source = "class Utf16 {}\r\n";
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(source)).ToArray();
        await File.WriteAllBytesAsync(Path.Combine(temp.Path, "utf16.cs"), bytes);

        var result = await CreateScanner().ScanAsync(temp.Path, null, Options());

        var file = Assert.Single(result.Files);
        Assert.Equal(source, file.Content);
        Assert.Equal(bytes.Length, file.OriginalBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), file.Sha256);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Utf32BomIsDecodedAndCountedAsText(bool bigEndian)
    {
        using var temp = new TemporaryDirectory();
        const string source = "class Utf32 {}\r\nsecond line\n";
        var encoding = new UTF32Encoding(bigEndian, byteOrderMark: true, throwOnInvalidCharacters: true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(source)).ToArray();
        var path = Path.Combine(temp.Path, "utf32.cs");
        await File.WriteAllBytesAsync(path, bytes);

        var scanner = CreateScanner();
        var contentResult = await scanner.ScanAsync(temp.Path, null, Options());
        var file = Assert.Single(contentResult.Files);
        Assert.Equal(source, file.Content);
        Assert.Equal(bytes.Length, file.OriginalBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), file.Sha256);

        var lineResult = await scanner.ScanAsync(temp.Path, null, new ScanOptions(250_000, true, false, false, false, null, true));
        Assert.Equal(2, Assert.Single(lineResult.LineCounts).Lines);
    }

    [Fact]
    public async Task InvalidUtf8IsReportedInsteadOfSilentlyReplaced()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllBytesAsync(Path.Combine(temp.Path, "invalid.cs"), [0xC3, 0x28]);

        var result = await CreateScanner().ScanAsync(temp.Path, null, Options());

        Assert.Empty(result.Files);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task SafetyFiltersSkipOversizedBinaryAndPreviousOutputFiles()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "a-large.cs"), new string('x', 101));
        await File.WriteAllBytesAsync(Path.Combine(temp.Path, "b-binary.cs"), [0x41, 0x00, 0x42]);
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "c-output.cs"), "CODECAT_VERSION: 1\n");
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "d-good.cs"), "class Good {}\n");

        var result = await CreateScanner().ScanAsync(temp.Path, null, new ScanOptions(100, true, false, false, false, null));

        Assert.Equal("d-good.cs", Assert.Single(result.Files).RelativePath);
        Assert.Equal(1, result.SkippedByReason["too_large"]);
        Assert.Equal(1, result.SkippedByReason["binary"]);
        Assert.Equal(1, result.SkippedByReason["codecat_output"]);
    }

    [Fact]
    public void OutputPathComparisonFollowsHostFilesystemCaseRules()
    {
        var root = Path.GetTempPath();
        var input = Path.Combine(root, "Codecat-Case-Test.txt");
        var output = Path.Combine(root, "codecat-case-test.txt");
        var reason = GlobalScanRules.TryDenyFileBeforePluginMatch(input, input, output);

        Assert.Equal(PathRules.Comparer.Equals(input, output) ? "output_file" : null, reason);
    }

    [Fact]
    public void TreeReportFollowsHostFilesystemCaseRules()
    {
        using var output = new StringWriter();
        LineReportWriter.Write(output,
        [
            new FileLineCount("A/one.cs", 1),
            new FileLineCount("a/two.cs", 2)
        ], tree: true, depth: null);

        var report = output.ToString();
        var distinctDirectories = report.Contains("  A/", StringComparison.Ordinal)
            && report.Contains("  a/", StringComparison.Ordinal);
        Assert.Equal(!PathRules.Comparer.Equals("A", "a"), distinctDirectories);
    }

    [Theory]
    [InlineData("a\rb\r", 2)]
    [InlineData("a\r\nb\r\n", 2)]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    public void MetricsCountAllCommonLineEndings(string content, int expectedLines)
    {
        var metrics = new MetricsCalculator().Calculate(content, content, Encoding.UTF8.GetByteCount(content), "hash");
        Assert.Equal(expectedLines, metrics.Lines);
        Assert.Equal(expectedLines, metrics.OriginalLines);
    }

    private static ScanOptions Options(bool useGitignore = false) => new(250_000, true, false, false, useGitignore, null);

    private static ScanOrchestrator CreateScanner(IFileReader? reader = null) => new(
        new PluginRegistry(), new FileProcessor(reader ?? new FileReader(), new FileLineCounter(), new MinifierRegistry(), new MetricsCalculator()),
        new ScanErrorHandler<ScanError>(NullLogger<ScanErrorHandler<ScanError>>.Instance), NullLogger<FileTreeWalker>.Instance);

    private sealed class FailingFileReader(string fileName) : IFileReader
    {
        private readonly FileReader _inner = new();

        public Task<Result<FileReadResult, ScanError>> ReadAsync(string fullPath, string relativePath, long maxFileBytes, CancellationToken cancellationToken) =>
            Path.GetFileName(fullPath) == fileName
                ? Task.FromResult(Result<FileReadResult, ScanError>.Failure(new ScanError(ScanErrorKind.ReadFile, relativePath, "simulated read failure")))
                : _inner.ReadAsync(fullPath, relativePath, maxFileBytes, cancellationToken);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codecat-test-{Guid.NewGuid():N}");
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
