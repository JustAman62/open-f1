namespace OpenF1.Console;

public class SwitchToLogsInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main];

    public ConsoleKey[] Keys => [ConsoleKey.L];

    public string Description => "Logs";

    public int Sort => 62;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);
        state.CurrentScreen = Screen.Logs;
    }
}
