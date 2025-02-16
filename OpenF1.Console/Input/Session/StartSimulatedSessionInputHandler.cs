using Microsoft.Extensions.Options;
using OpenF1.Data;
using Spectre.Console;

namespace OpenF1.Console;

public class StartSimulatedSessionInputHandler(
    IJsonTimingClient jsonTimingClient,
    IOptions<LiveTimingOptions> options
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey[] Keys => [ConsoleKey.F];

    public string Description => "Start Simulation";

    public int Sort => 41;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var directories = jsonTimingClient.GetDirectoryNames();
        var title = $"""
            Select the data directory to run the simulation from. 
            If you cannot see your directory here, ensure that it contains both a file named subscribe.txt and live.txt.
            To change this directory, set the OPENF1_DATADIRECTORY environment variable.
            Configured Directory: {options.Value.DataDirectory}
            """;

        var prompt = new SelectionPrompt<string>()
            .Title(title)
            .AddChoices("Cancel")
            .AddChoices(directories);

        AnsiConsole.Clear();

        var result = AnsiConsole.Prompt(prompt);
        if (result == "Cancel")
        {
            return Task.CompletedTask;
        }

        _ = jsonTimingClient.StartAsync(result);
        return Task.CompletedTask;
    }
}
