using System.Runtime.InteropServices;
using System.Runtime.Loader;
using InMemLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Join(LiveTimingOptions.BaseDirectory, "config.json"), optional: true)
    .AddEnvironmentVariables("OPENF1_")
    .Build();

var serviceCollection = new ServiceCollection()
    .AddOptions()
    .AddLogging(configure => configure.ClearProviders().AddInMemory())
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTiming(configuration);

var services = serviceCollection.BuildServiceProvider();

var notifyService = services.GetRequiredService<INotifyService>();
// When notifications are received, send a ASCII BEL to the console to make a noise to alert the user.
notifyService.RegisterNotificationHandler(() => Console.Write("\u0007"));

var consoleLoop = services.GetRequiredService<ConsoleLoop>();

// Register handler for exists and SIGTERM to clean up on app exit
AppDomain.CurrentDomain.ProcessExit += UnloadOnSigTerm;
Console.CancelKeyPress += UnloadOnSigTerm;

void UnloadOnSigTerm(object? sender, EventArgs e)
{
    // Cleanup our running services and reset terminal config
    consoleLoop.Stop();
    Console.Clear();
    Console.WriteLine("Exiting openf1-console...");
    AnsiConsole.Cursor.Show();
}

await consoleLoop.ExecuteAsync();
