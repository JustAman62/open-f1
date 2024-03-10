namespace OpenF1.Console;

public class SwitchToTimingOverviewInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;
    
    public Screen[] ApplicableScreens => [Screen.Main, Screen.TimingHistory];

    public ConsoleKey ConsoleKey => ConsoleKey.O;

    public string Description => "Timing Overview";

    public int Sort => 52;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingOverview;
        return Task.CompletedTask;
    }
}
