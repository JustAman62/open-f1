using OpenF1.Data;

namespace OpenF1.Console;

public class SwitchPageInputHandler(LapCountProcessor lapCountProcessor, State state)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        [Screen.RaceControl, Screen.TeamRadio, Screen.TimingTower, Screen.TimingHistory, Screen.ChampionshipStats];

    public ConsoleKey[] Keys => [ConsoleKey.LeftArrow, ConsoleKey.RightArrow];

    public string Description => $"Page {GetScreenIndex() + 1}";

    public int Sort => 20;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        // Find the index of the current screen, and move to the next one
        var index = GetScreenIndex();
        var newIndex = consoleKeyInfo.Key == ConsoleKey.LeftArrow ? index - 1 : index + 1;
        var newScreen = newIndex % ApplicableScreens.Length;
        state.CurrentScreen =
            newScreen < 0 ? ApplicableScreens.Last() : ApplicableScreens[newScreen];

        // Depending on the new screen, reset the cursor to a useful position
        switch (state.CurrentScreen)
        {
            case Screen.TimingHistory:
                state.CursorOffset = Math.Max(lapCountProcessor.Latest.CurrentLap - 2 ?? 1, 1);
                break;
            case Screen.TimingTower:
            case Screen.RaceControl:
            case Screen.ChampionshipStats:
            case Screen.TeamRadio:
                state.CursorOffset = 0;
                break;
        }

        return Task.CompletedTask;
    }

    private int GetScreenIndex() => ApplicableScreens.ToList().IndexOf(state.CurrentScreen);
}
