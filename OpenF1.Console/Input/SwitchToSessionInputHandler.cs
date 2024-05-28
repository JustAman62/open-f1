namespace OpenF1.Console;

public class SwitchToSessionInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main];

    public ConsoleKey[] Keys => [ConsoleKey.S];

    public string Description => "Session";

    public int Sort => 60;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.ManageSession;
        return Task.CompletedTask;
    }
}
