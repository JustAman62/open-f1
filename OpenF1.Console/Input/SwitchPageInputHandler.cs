using OpenF1.Data;

namespace OpenF1.Console;

public class SwitchPageInputHandler(LapCountProcessor lapCountProcessor, State state)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        [Screen.RaceControl, Screen.DriverTracker, Screen.TimingTower, Screen.TimingHistory];

    public ConsoleKey[] Keys => [ConsoleKey.LeftArrow, ConsoleKey.RightArrow];

    public string Description => "Page";

    public int Sort => 54;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        // Find the index of the current screen, and move to the next one
        var index = ApplicableScreens.ToList().IndexOf(state.CurrentScreen);
        var newIndex = consoleKeyInfo.Key == ConsoleKey.LeftArrow ? index - 1 : index + 1;
        var newScreen = newIndex % ApplicableScreens.Length;
        state.CurrentScreen =
            newScreen < 0 ? ApplicableScreens.Last() : ApplicableScreens[newScreen];

        // Depending on the new screen, reset the cursor to a useful position
        switch (state.CurrentScreen)
        {
            case Screen.TimingHistory:
                state.CursorOffset = lapCountProcessor.Latest?.CurrentLap - 2 ?? state.CursorOffset;
                break;
            case Screen.TimingTower:
            case Screen.RaceControl:
            case Screen.DriverTracker:
                state.CursorOffset = 0;
                break;
        }

        return Task.CompletedTask;
    }
}
