using OpenF1.Console;
using Spectre.Console;

public class SwitchToSimulatedSessionInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.S;

    public string Description => "Start Simulation";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.ManageSimulatedSession;
        return Task.CompletedTask;
    }
}
