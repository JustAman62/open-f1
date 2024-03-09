using InMemLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;

var serviceCollection = new ServiceCollection()
    .AddOptions()
    .AddLogging(configure => configure.ClearProviders().AddInMemory())
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTimingProvider();

var services = serviceCollection.BuildServiceProvider();

var consoleLoop = services.GetRequiredService<ConsoleLoop>();

var layout = new Layout("Root");
await consoleLoop.ExecuteAsync();
