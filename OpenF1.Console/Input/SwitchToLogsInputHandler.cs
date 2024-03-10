namespace OpenF1.Console;

public class SwitchToLogsInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.L;

    public string Description => "Logs";

    public int Sort => 54;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.Logs;
        return Task.CompletedTask;
    }
}
