using InMemLogger;
using OpenF1.Console;
using OpenF1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile(Path.Join(LiveTimingOptions.BaseDirectory, "config.json"), optional: true)
    .AddEnvironmentVariables("OPENF1_")
    .Build();

builder.Services
    .AddOptions()
    .AddLogging(configure => configure.ClearProviders().AddInMemory())
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTiming(builder.Configuration)
    .AddSingleton<INotifyHandler, NotifyHandler>()
    .AddSingleton<ConsoleLoop>()
    .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>());

var app = builder.Build();

await app.RunAsync();
