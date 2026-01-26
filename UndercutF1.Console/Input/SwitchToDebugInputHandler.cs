using Microsoft.Extensions.Options;

namespace UndercutF1.Console;

public class SwitchToDebugInputHandler(State state, IOptions<ConsoleOptions> Options)
    : IInputHandler
{
    public bool IsEnabled => Options.Value.Verbose;

    public Screen[] ApplicableScreens => [Screen.Main];

    public ConsoleKey[] Keys => [ConsoleKey.D];

    public string Description => "Debug View";

    public int Sort => 68;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);
        state.CurrentScreen = Screen.DebugData;
    }
}
