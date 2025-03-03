using OpenF1.Data;

namespace OpenF1.Console;

public class StartSimulatedSessionInputHandler(
    IJsonTimingClient jsonTimingClient,
    StartSimulatedSessionOptions displayOptions,
    State state
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.StartSimulatedSession];

    public ConsoleKey[] Keys => [ConsoleKey.Enter, ConsoleKey.RightArrow, ConsoleKey.L];

    public ConsoleKey[] DisplayKeys => [ConsoleKey.RightArrow];

    public string Description => "Select";

    public int Sort => 41;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var directories = await jsonTimingClient.GetDirectoryNamesAsync();
        if (
            displayOptions.SelectedLocation is null
            && state.CursorOffset >= 0
            && state.CursorOffset < directories.Count
        )
        {
            displayOptions.SelectedLocation = state.CursorOffset;
            state.CursorOffset = 0;
            return;
        }

        if (displayOptions.SelectedLocation.HasValue)
        {
            var selected = directories.ElementAt(displayOptions.SelectedLocation.Value).Value;
            if (state.CursorOffset >= 0 && state.CursorOffset < selected.Count)
            {
                _ = jsonTimingClient.StartAsync(selected.ElementAt(state.CursorOffset).Directory);
                state.CurrentScreen = Screen.TimingTower;
                state.CursorOffset = 0;
            }
        }
    }
}

public class StartSimulatedSessionDeselectInputHandler(
    StartSimulatedSessionOptions displayOptions,
    State state
) : IInputHandler
{
    public bool IsEnabled => displayOptions.SelectedLocation.HasValue;

    public Screen[] ApplicableScreens => [Screen.StartSimulatedSession];

    public ConsoleKey[] Keys => [ConsoleKey.LeftArrow, ConsoleKey.H];

    public ConsoleKey[] DisplayKeys => [ConsoleKey.LeftArrow];

    public string Description => "Deselect";

    public int Sort => 42;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        state.CursorOffset = displayOptions.SelectedLocation.GetValueOrDefault();
        displayOptions.SelectedLocation = null;

        return Task.CompletedTask;
    }
}
