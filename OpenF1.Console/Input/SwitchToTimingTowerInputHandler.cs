namespace OpenF1.Console;

public class SwitchToTimingTowerInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;
    
    public Screen[] ApplicableScreens => [Screen.Main, Screen.RaceControl, Screen.TimingHistory];

    public ConsoleKey ConsoleKey => ConsoleKey.O;

    public string Description => "Timing Tower";

    public int Sort => 52;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingTower;
        return Task.CompletedTask;
    }
}
