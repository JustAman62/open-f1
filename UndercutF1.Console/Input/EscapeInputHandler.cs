namespace UndercutF1.Console;

public class EscapeInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey[] Keys =>
        state.CurrentScreen == Screen.Main
            ? [ConsoleKey.Q, ConsoleKey.X, (ConsoleKey)3]
            : [ConsoleKey.Escape, (ConsoleKey)3, ConsoleKey.F16]; // ConsoleKey.F16 is actually Backspace

    public ConsoleKey[] DisplayKeys =>
        [state.CurrentScreen == Screen.Main ? ConsoleKey.Q : ConsoleKey.Escape];

    public int Sort => 1;

    public string Description =>
        state.CurrentScreen switch
        {
            Screen.Main => "Quit",
            _ => "Back",
        };

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);

        state.CurrentScreen = state.CurrentScreen switch
        {
            Screen.Main => Screen.Shutdown,
            Screen.StartSimulatedSession => Screen.ManageSession,
            Screen.DownloadTranscriptionModel => Screen.TeamRadio,
            Screen.SelectDriver => state.PreviousScreen,
            Screen.ManageAccount => state.PreviousScreen,
            _ => Screen.Main,
        };

        state.CursorOffset = 0;
    }
}
