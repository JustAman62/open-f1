namespace OpenF1.Console;

public class SwitchToSessionInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.S;

    public string Description => "Session";

    public int Sort => 50;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.ManageSession;
        return Task.CompletedTask;
    }
}