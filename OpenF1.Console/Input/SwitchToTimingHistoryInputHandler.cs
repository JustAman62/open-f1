namespace OpenF1.Console;

public class SwitchToTimingHistoryInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;
    
    public Screen[] ApplicableScreens => [Screen.Main, Screen.TimingOverview];

    public ConsoleKey ConsoleKey => ConsoleKey.H;

    public string Description => "Timing by Lap";

    public int Sort => 53;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingHistory;
        return Task.CompletedTask;
    }
}
