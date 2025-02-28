namespace OpenF1.Console;

public class EscapeInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey[] Keys =>
        state.CurrentScreen == Screen.Main
            ? [ConsoleKey.Q, ConsoleKey.X, (ConsoleKey)3]
            : [ConsoleKey.Escape, (ConsoleKey)3];

    public ConsoleKey[] DisplayKeys =>
        [state.CurrentScreen == Screen.Main ? ConsoleKey.Q : ConsoleKey.Escape];

    public int Sort => 1;

    public string Description =>
        state.CurrentScreen switch
        {
            Screen.Main => "Quit",
            _ => "Back",
        };

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        state.CurrentScreen = state.CurrentScreen switch
        {
            Screen.Main => Screen.Shutdown,
            Screen.StartSimulatedSession => Screen.ManageSession,
            _ => Screen.Main,
        };

        return Task.CompletedTask;
    }
}
