namespace OpenF1.Console;

public class SwitchToTimingOverviewInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main, Screen.TimingHistory];

    public ConsoleKey ConsoleKey => ConsoleKey.O;

    public string Description => "Timing Overview";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingOverview;
        return Task.CompletedTask;
    }
}
