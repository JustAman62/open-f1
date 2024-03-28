using InMemLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Join(LiveTimingOptions.BaseDirectory, "config.json"))
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection()
    .AddOptions()
    .AddLogging(configure => configure.ClearProviders().AddInMemory())
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTiming(configuration.GetSection(LiveTimingOptions.ConfigurationSectionName));

var services = serviceCollection.BuildServiceProvider();

var consoleLoop = services.GetRequiredService<ConsoleLoop>();

var layout = new Layout("Root");
await consoleLoop.ExecuteAsync();
