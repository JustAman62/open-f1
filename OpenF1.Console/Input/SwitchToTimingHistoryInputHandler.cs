using OpenF1.Data;

namespace OpenF1.Console;

public class SwitchToTimingHistoryInputHandler(LapCountProcessor lapCountProcessor, State state)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Main, Screen.RaceControl, Screen.TimingTower];

    public ConsoleKey ConsoleKey => ConsoleKey.H;

    public string Description => "Timing by Lap";

    public int Sort => 53;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CurrentScreen = Screen.TimingHistory;

        // Automatically seek to the last lap we'll have history for (-1 to account for 1-indexing of lap count, then -1 again for previous lap)
        state.CursorOffset = lapCountProcessor.Latest?.CurrentLap - 2 ?? state.CursorOffset;
        return Task.CompletedTask;
    }
}
