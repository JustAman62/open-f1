namespace OpenF1.Console;

public class EscapeInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey[] Keys =>
        [state.CurrentScreen == Screen.Main ? ConsoleKey.X : ConsoleKey.Escape];

    public int Sort => 99;

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
