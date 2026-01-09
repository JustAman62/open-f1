using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace UndercutF1.Data;

public class SessionInfoProcessor(
    IHttpClientFactory httpClientFactory,
    ILogger<SessionInfoProcessor> logger
) : ProcessorBase<SessionInfoDataPoint>()
{
    private Task? _loadCircuitTask = null;

    public override void Process(SessionInfoDataPoint data)
    {
        base.Process(data);

        if (
            data.CircuitPoints.Count == 0
            && data.Meeting?.Circuit?.Key is not null
            && _loadCircuitTask is null
        )
        {
            // Load circuit points from the external API as it hasn't been loaded yet
            _loadCircuitTask = Task.Run(() =>
                LoadCircuitPoints(data.Meeting!.Circuit!.Key.Value, data.StartDate)
            );
        }
    }

    private async Task LoadCircuitPoints(int circuitKey, DateTime? eventDate)
    {
        eventDate ??= DateTime.UtcNow;
        try
        {
            logger.LogInformation("Loading circuit data for key {CircuitKey}", circuitKey);
            var httpClient = httpClientFactory.CreateClient("Proxy");
            var url = $"/api/v1/circuits/{circuitKey}/{DateTimeOffset.UtcNow.Year}";
            var circuitInfo = await httpClient
                .GetFromJsonAsync(url, SessionInfoProcessorContext.Default.CircuitInfoResponse)
                .ConfigureAwait(false);

            logger.LogDebug(
                "Received circuit info from MultiViewer: {CircuitInfo}",
                JsonSerializer.Serialize(
                    circuitInfo,
                    SessionInfoProcessorContext.Default.CircuitInfoResponse
                )
            );

            Latest.CircuitPoints = circuitInfo!.X.Zip(circuitInfo.Y).ToList();
            Latest.CircuitCorners = circuitInfo
                .Corners.Select(x => (x.Number, x.TrackPosition.X, x.TrackPosition.Y))
                .ToList();
            Latest.CircuitRotation = circuitInfo!.Rotation;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load circuit data for key {CircuitKey}", circuitKey);
        }
    }

    internal sealed record CircuitInfoResponse(
        List<int> X,
        List<int> Y,
        List<TrackCornerResponse> Corners,
        int Rotation
    );

    internal sealed record TrackCornerResponse(int Number, TrackPositionResponse TrackPosition);

    internal sealed record TrackPositionResponse(float X, float Y);
}

/// <summary>
/// A JsonConverter that handles ints that are formatted as floats (e.g. 1023.0).
/// If the value is a real float (e.g. 1023.04) then this converter will delegate
/// to <see cref="Utf8JsonReader.GetInt32()"/> which will throw a <see cref="JsonException"/>.
/// </summary>
internal class IntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var valDouble = reader.GetDouble();
        var valInt = (int)valDouble;
        return valDouble == valInt ? valInt : reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(IntJsonConverter)]
)]
[JsonSerializable(typeof(SessionInfoProcessor.CircuitInfoResponse))]
internal partial class SessionInfoProcessorContext : JsonSerializerContext { }
