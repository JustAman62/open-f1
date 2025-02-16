using OpenF1.Data;

namespace OpenF1.Console;

public class SwitchToStartSimulatedSessionInputHandler(
    SessionInfoProcessor sessionInfo,
    IJsonTimingClient jsonTimingClient,
    StartSimulatedSessionOptions displayOptions,
    State state
) : IInputHandler
{
    public bool IsEnabled => sessionInfo.Latest.Name is null;

    public Screen[] ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey[] Keys => [ConsoleKey.F];

    public string Description => "Start Simulated Session";

    public int Sort => 65;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);
        displayOptions.Sessions = await jsonTimingClient.GetDirectoryNamesAsync();
        state.CurrentScreen = Screen.StartSimulatedSession;
    }
}
