namespace OpenF1.Console;

public class SwitchToTimingTowerInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main, Screen.ManageSession];

    public ConsoleKey[] Keys => [ConsoleKey.T];

    public string Description => "Timing Tower";

    public int Sort => 61;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingTower;
        state.CursorOffset = 0;
        return Task.CompletedTask;
    }
}
