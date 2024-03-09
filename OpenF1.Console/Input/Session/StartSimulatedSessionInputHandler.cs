using OpenF1.Console;
using Spectre.Console;

public class StartSimulatedSessionInputHandler(IJsonTimingClient jsonTimingClient) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey ConsoleKey => ConsoleKey.S;

    public string Description => "Start Simulation";

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var fileNames = jsonTimingClient.GetFileNames();
        var prompt = new SelectionPrompt<string>()
            .Title("Choose which file to run the simulation from")
            .AddChoices(fileNames)
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
