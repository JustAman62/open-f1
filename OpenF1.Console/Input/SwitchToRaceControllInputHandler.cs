namespace OpenF1.Console;

public class SwitchToRaceControlInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;
    
    public Screen[] ApplicableScreens => [Screen.Main, Screen.TimingTower, Screen.TimingHistory];

    public ConsoleKey ConsoleKey => ConsoleKey.R;

    public string Description => "Race Control";

    public int Sort => 53;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.RaceControl;
        return Task.CompletedTask;
    }
}
