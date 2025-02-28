using System.Text.Json;
using System.Text.Json.Serialization;
using OpenF1.Data;
using TextCopy;

namespace OpenF1.Console;

public class CopyToClipboardInputHandler(
    IClipboard clipboard,
    IEnumerable<IProcessor> processors,
    State state
) : IInputHandler
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

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

        var latestDataPoint = processor
            .GetType()
            .GetProperty(nameof(IProcessor<TimingDataPoint>.Latest))
            ?.GetValue(processor);
        if (latestDataPoint is null)
            return;

        var serialized = JsonSerializer.Serialize(latestDataPoint, _jsonSerializerOptions);
        await clipboard.SetTextAsync(serialized, cancellationToken);
    }
}
