using OpenF1.Data;
using Spectre.Console;

namespace OpenF1.Console;

public class StartSimulatedSessionInputHandler(IJsonTimingClient jsonTimingClient) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey ConsoleKey => ConsoleKey.F;

    public string Description => "Start Simulation";

    public int Sort => 2;

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var directories = jsonTimingClient.GetDirectoryNames();
        var prompt = new SelectionPrompt<string>()
            .Title(
                "Choose which directory to run the simulation from. If you cannot see your directory here, ensure that it contains both a file named subscribe.txt and live.txt."
            )
            .AddChoices(directories)
            .AddChoices("Cancel");

        AnsiConsole.Clear();

        var result = AnsiConsole.Prompt(prompt);
        if (result == "Cancel")
        {
            return;
        }

        await jsonTimingClient.StartAsync(result);
    }
}
