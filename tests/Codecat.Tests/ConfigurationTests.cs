using Codecat.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Codecat.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void LoaderReadsOptionsWithoutReflectionBinding()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["scan:DefaultMaxFileBytes"] = "5000",
            ["scan:DefaultQuiet"] = "true",
            ["scan:DefaultExtensionFilter:0"] = "cs",
            ["scan:DefaultExtensionFilter:1"] = ".csproj"
        }).Build();

        var options = ScanConfigurationLoader.Load(configuration);

        Assert.Equal(5000, options.DefaultMaxFileBytes);
        Assert.True(options.DefaultQuiet);
        Assert.Equal(new[] { ".cs", ".csproj" }, options.DefaultExtensionFilter!.Order(StringComparer.OrdinalIgnoreCase));
    }
}
