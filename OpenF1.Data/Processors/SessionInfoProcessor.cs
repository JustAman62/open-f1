using System.Net.Http.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class SessionInfoProcessor(IMapper mapper, ILogger<SessionInfoProcessor> logger) : ProcessorBase<SessionInfoDataPoint>(mapper)
{
    public override void Process(SessionInfoDataPoint data)
    {
        base.Process(data);

        if (data.CircuitPoints.Count == 0 && data.Meeting?.Circuit?.Key is not null)
        {
            // Load circuit points from the external API as it hasn't been loaded yet
            _ = Task.Run(() => LoadCircuitPoints(data.Meeting!.Circuit!.Key.Value));
        }
    }

    private async Task LoadCircuitPoints(int circuitKey)
    {
        try
        {
            logger.LogInformation("Loading circuit data for key {CircuitKey}", circuitKey);
            using var httpClient = new HttpClient();
            var url = $"https://api.multiviewer.app/api/v1/circuits/{circuitKey}/{DateTimeOffset.UtcNow.Year}";
            var circuitInfo = await httpClient.GetFromJsonAsync<CircuitInfoResponse>(url).ConfigureAwait(false);
            Latest.CircuitPoints = circuitInfo!.X.Zip(circuitInfo.Y).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load circuit data for key {CircuitKey}", circuitKey);
        }
    }

    private sealed record CircuitInfoResponse(List<int> X, List<int> Y);
}
