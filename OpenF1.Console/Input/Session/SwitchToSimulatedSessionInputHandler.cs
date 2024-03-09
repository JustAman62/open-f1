using OpenF1.Console;

public class SwitchToSessionInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.S;

    public string Description => "Session";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.ManageSession;
        return Task.CompletedTask;
    }
}
