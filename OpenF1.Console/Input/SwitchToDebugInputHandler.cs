using Microsoft.Extensions.Options;
using OpenF1.Data;

namespace OpenF1.Console;

public class SwitchToDebugInputHandler(State state, IOptions<LiveTimingOptions> liveTimingOptions)
    : IInputHandler
{
    public bool IsEnabled => liveTimingOptions.Value.Verbose;

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
