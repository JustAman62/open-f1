using System.Net.Http.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class SessionInfoProcessor(IMapper mapper, ILogger<SessionInfoProcessor> logger)
    : ProcessorBase<SessionInfoDataPoint>(mapper)
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
            _loadCircuitTask = Task.Run(
                () => LoadCircuitPoints(data.Meeting!.Circuit!.Key.Value, data.StartDate)
            );
        }
    }

    private async Task LoadCircuitPoints(int circuitKey, DateTime? eventDate)
    {
        eventDate ??= DateTime.UtcNow;
        try
        {
            logger.LogInformation("Loading circuit data for key {CircuitKey}", circuitKey);
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"open-f1/{ThisAssembly.AssemblyInformationalVersion}"
            );
            var url =
                $"https://api.multiviewer.app/api/v1/circuits/{circuitKey}/{DateTimeOffset.UtcNow.Year}";
            var circuitInfo = await httpClient
                .GetFromJsonAsync<CircuitInfoResponse>(url)
                .ConfigureAwait(false);

            Latest.CircuitPoints = circuitInfo!.X.Zip(circuitInfo.Y).ToList();
            Latest.CircuitCorners = circuitInfo
                .Corners.Select(x => (x.Number, x.TrackPosition.X, x.TrackPosition.Y))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load circuit data for key {CircuitKey}", circuitKey);
        }
    }

    private sealed record CircuitInfoResponse(
        List<int> X,
        List<int> Y,
        List<TrackCornerResponse> Corners
    );

    private sealed record TrackCornerResponse(int Number, TrackPositionResponse TrackPosition);

    private sealed record TrackPositionResponse(float X, float Y);
}
