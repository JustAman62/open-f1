using Microsoft.Extensions.DependencyInjection;
using OpenF1.Console;
using Spectre.Console;

var serviceCollection = new ServiceCollection()
    .AddOptions()
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddSingleton<IInputHandler, TestInputHandler>()
    .AddSingleton<TestDisplay>();

var services = serviceCollection.BuildServiceProvider();

var consoleLoop = services.GetRequiredService<ConsoleLoop>();

var layout = new Layout("Root");
await AnsiConsole
    .Live(layout)
    .StartAsync(consoleLoop.ExecuteAsync);
