namespace OpenF1.Console;

public class SwitchToTimingHistoryInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main, Screen.TimingOverview];

    public ConsoleKey ConsoleKey => ConsoleKey.H;

    public string Description => "Timing by Lap";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingHistory;
        return Task.CompletedTask;
    }
}
