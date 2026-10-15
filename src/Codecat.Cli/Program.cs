using Codecat.Minification;
using Codecat.Output;
using Codecat.Plugins;
using Codecat.Scanning;
using Codecat.Cli;
using Codecat.Cli.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var hostBuilder = Host.CreateApplicationBuilder(args);

var scanConfiguration = ScanConfigurationLoader.Load(hostBuilder.Configuration);
hostBuilder.Services.AddSingleton<IOptions<ScanConfiguration>>(Options.Create(scanConfiguration));

hostBuilder.Services.AddTransient<Func<PluginSelection, IScanOrchestrator>>(sp =>
{
    var defaultIncludeAll = sp.GetRequiredService<IOptions<ScanConfiguration>>().Value.DefaultAllPlugins;
    return selection =>
    {
        return new ScanOrchestrator(
            new PluginRegistry(selection with { IncludeAllSources = defaultIncludeAll || selection.IncludeAllSources }),
            sp.GetRequiredService<FileProcessor>(),
            sp.GetRequiredService<IErrorHandler<ScanError>>(),
            sp.GetRequiredService<ILogger<FileTreeWalker>>());
    };
});

hostBuilder.Services.AddSingleton<IFileReader, FileReader>();
hostBuilder.Services.AddSingleton<IFileLineCounter, FileLineCounter>();
hostBuilder.Services.AddSingleton<IMinifierRegistry, MinifierRegistry>();
hostBuilder.Services.AddSingleton<IMetricsCalculator, MetricsCalculator>();
hostBuilder.Services.AddTransient<FileProcessor>();
hostBuilder.Services.AddSingleton<IScanResultWriter, CodecatWriter>();
hostBuilder.Services.AddSingleton<IClipboardService, ClipboardCopier>();
hostBuilder.Services.AddSingleton(typeof(IErrorHandler<>), typeof(ScanErrorHandler<>));
hostBuilder.Services.AddSingleton<CliRunner>();

hostBuilder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

using var host = hostBuilder.Build();
var runner = host.Services.GetRequiredService<CliRunner>();
var cancellationToken = CancellationToken.None;
return await runner.RunAsync(args, cancellationToken);
