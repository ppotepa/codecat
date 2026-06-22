using Codecat.Minification;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;
using Codecat.Cli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var hostBuilder = Host.CreateApplicationBuilder(args);

hostBuilder.Services.AddOptions<ScanConfiguration>()
    .Bind(hostBuilder.Configuration.GetSection("scan"));

hostBuilder.Services.AddTransient<Func<bool, IScanOrchestrator>>(sp =>
{
    var defaultIncludeAll = sp.GetRequiredService<IOptions<ScanConfiguration>>().Value.DefaultAllPlugins;
    return includeAll =>
    {
        return new ScanOrchestrator(
            new PluginRegistry(defaultIncludeAll || includeAll),
            sp.GetRequiredService<IFileReader>(),
            sp.GetRequiredService<IMinifierRegistry>(),
            sp.GetRequiredService<IMetricsCalculator>(),
            sp.GetRequiredService<IErrorHandler<ScanError>>(),
            sp.GetRequiredService<ILogger<ScanOrchestrator>>());
    };
});

hostBuilder.Services.AddSingleton<IFileReader, FileReader>();
hostBuilder.Services.AddSingleton<IMinifierRegistry, MinifierRegistry>();
hostBuilder.Services.AddSingleton<IMetricsCalculator, MetricsCalculator>();
hostBuilder.Services.AddSingleton<IScanResultWriter, CodecatWriter>();
hostBuilder.Services.AddSingleton(typeof(IErrorHandler<>), typeof(ScanErrorHandler<>));
hostBuilder.Services.AddSingleton<CliRunner>();

hostBuilder.Logging.AddSimpleConsole();

using var host = hostBuilder.Build();
var runner = host.Services.GetRequiredService<CliRunner>();
var cancellationToken = CancellationToken.None;
return await runner.RunAsync(args, cancellationToken);
