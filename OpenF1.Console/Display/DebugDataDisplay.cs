using System.Text.Json;
using System.Text.Json.Serialization;
using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public sealed class DebugDataDisplay(IEnumerable<IProcessor> processors, State state) : IDisplay
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public Screen Screen => Screen.DebugData;

    public Task<IRenderable> GetContentAsync()
    {
        var processor = processors.ElementAtOrDefault(state.CursorOffset);
        if (processor is null)
            return Task.FromResult<IRenderable>(
                new Text($"No processor for {state.CursorOffset} cursor offset")
            );
        var processorName = processor.GetType().Name;

        var latestDataPoint = processor
            .GetType()
            .GetProperty(nameof(IProcessor<TimingDataPoint>.Latest))
            ?.GetValue(processor);
        if (latestDataPoint is null)
            return Task.FromResult<IRenderable>(
                new Text($"Latest datapoint for {processorName} is null")
            );

        var rows = new Rows(
            new Text($"Name: {processorName}"),
            new Text(JsonSerializer.Serialize(latestDataPoint, _jsonSerializerOptions))
        );

        return Task.FromResult<IRenderable>(rows);
    }
}
