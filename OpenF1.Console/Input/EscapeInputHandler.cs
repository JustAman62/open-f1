namespace OpenF1.Console;

public class EscapeInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.Escape;

    public string Description =>
        state.CurrentScreen switch
        {
            Screen.Main => "Exit",
            _ => "Return"
        };

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = state.CurrentScreen switch
        {
            Screen.Main => Screen.Shutdown,
            _ => Screen.Main
        };

        return Task.CompletedTask;
    }
}
