using UndercutF1.Data;

namespace UndercutF1.Console;

public class SwitchPageInputHandler(TimingDataProcessor timingDataProcessor, State state)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        [
            Screen.RaceControl,
            Screen.TeamRadio,
            Screen.DriverTracker,
            Screen.TimingTower,
            Screen.TimingHistory,
            Screen.TyreStints,
            Screen.ChampionshipStats,
        ];

    public ConsoleKey[] Keys =>
        [ConsoleKey.LeftArrow, ConsoleKey.H, ConsoleKey.RightArrow, ConsoleKey.L];

    public ConsoleKey[] DisplayKeys => [ConsoleKey.LeftArrow, ConsoleKey.RightArrow];

    public string Description => $"Page {GetScreenIndex() + 1}";

    public int Sort => 20;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);

        // Find the index of the current screen, and move to the next one
        var index = GetScreenIndex();
        var newIndex = consoleKeyInfo.Key is ConsoleKey.LeftArrow or ConsoleKey.H
            ? index - 1
            : index + 1;
        var newScreen = newIndex % ApplicableScreens.Length;
        state.CurrentScreen =
            newScreen < 0 ? ApplicableScreens.Last() : ApplicableScreens[newScreen];

        // Depending on the new screen, reset the cursor to a useful position
        switch (state.CurrentScreen)
        {
            case Screen.TimingHistory:
                state.CursorOffset = Math.Max(GetLatestLap() - 1, 1);
                break;
            case Screen.TimingTower:
            case Screen.RaceControl:
            case Screen.DriverTracker:
            case Screen.ChampionshipStats:
            case Screen.TeamRadio:
            case Screen.TyreStints:
                state.CursorOffset = 0;
                break;
        }
    }

    private int GetScreenIndex() => ApplicableScreens.ToList().IndexOf(state.CurrentScreen);

    private int GetLatestLap() =>
        timingDataProcessor
            .DriversByLap.Where(x => x.Value.Count > 0)
            .DefaultIfEmpty()
            .MaxBy(x => x.Key)
            .Key;
}
