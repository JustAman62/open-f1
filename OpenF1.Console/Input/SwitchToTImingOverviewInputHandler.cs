namespace OpenF1.Console;

public class SwitchToTImingOverviewInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.O;

    public string Description => "Timing Overview";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingOverview;
        return Task.CompletedTask;
    }
}
