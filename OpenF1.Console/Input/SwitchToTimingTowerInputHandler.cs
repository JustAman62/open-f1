namespace OpenF1.Console;

public class SwitchToTimingTowerInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main, Screen.ManageSession];

    public ConsoleKey[] Keys => [ConsoleKey.T];

    public string Description => "Timing Tower";

    public int Sort => 61;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);
        state.CurrentScreen = Screen.TimingTower;
        state.CursorOffset = 0;
    }
}
