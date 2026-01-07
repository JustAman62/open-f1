using System.Text.Json;
using TextCopy;
using UndercutF1.Data;

namespace UndercutF1.Console;

public class CopyToClipboardInputHandler(
    IClipboard clipboard,
    IEnumerable<IProcessor> processors,
    State state
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.DebugData];

    public ConsoleKey[] Keys => [ConsoleKey.C];

    public string Description => "Copy To Clipboard";

    public int Sort => 40;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var processor = processors.ElementAtOrDefault(state.CursorOffset);
        if (processor is null)
            return;

        var latestDataPoint = processor.GetLatestData();
        if (latestDataPoint is null)
            return;

        var serialized = JsonSerializer.Serialize(
            latestDataPoint,
            TimingDataSerializerContext.Pretty.GetTypeInfo(latestDataPoint.GetType())!
        );
        await clipboard.SetTextAsync(serialized, cancellationToken);
    }
}
