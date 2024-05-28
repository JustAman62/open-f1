using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenF1.Data;

public class JsonTimingClient(
    ITimingService timingService,
    IDateTimeProvider dateTimeProvider,
    IOptions<LiveTimingOptions> options,
    ILogger<JsonTimingClient> logger
) : IJsonTimingClient
{
    public Task? ExecuteTask { get; private set; }

    /// <inheritdoc />
    public IEnumerable<string> GetDirectoryNames()
    {
        Directory.CreateDirectory(options.Value.DataDirectory);
        return Directory
            .GetDirectories(options.Value.DataDirectory)
            .Where(x =>
                Directory
                    .GetFiles(x)
                    .Any(x => x.EndsWith("live.txt", StringComparison.OrdinalIgnoreCase))
                && Directory
                    .GetFiles(x)
                    .Any(x => x.EndsWith("subscribe.txt", StringComparison.OrdinalIgnoreCase))
            )
            .OrderByDescending(x => Directory.GetCreationTime(x));
    }

    /// <inheritdoc />
    public Task StartAsync(string directory, CancellationToken cancellationToken)
    {
        ExecuteTask = LoadSimulationDataAsync(directory, cancellationToken);
        return ExecuteTask;
    }

    private async Task LoadSimulationDataAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            // Handle the dump of data we receive at subscription time
            var subscriptionData = await File.ReadAllTextAsync(
                    Path.Join(directory, "/subscribe.txt"),
                    cancellationToken
                )
                .ConfigureAwait(false);

            timingService.ProcessSubscriptionData(subscriptionData);

            var subscriptionHeartbeat = JsonNode
                .Parse(subscriptionData)
                ?["Heartbeat"]?.Deserialize<HeartbeatDataPoint>();
            if (subscriptionHeartbeat is not null)
            {
                dateTimeProvider.Delay = DateTimeOffset.UtcNow - subscriptionHeartbeat.Utc;
                logger.LogInformation(
                    $"Calculated a delay of {dateTimeProvider.Delay} between now and {subscriptionHeartbeat.Utc:s}"
                );
            }
            else
            {
                logger.LogError($"Unable to calculate a delay for this simulation data");
            }

            // Handle the real-time data
            var lines = File.ReadLinesAsync(Path.Join(directory, "/live.txt"), cancellationToken);

            await foreach (var line in lines)
            {
                try
                {
                    var (type, data, timestamp) = ProcessLine(line);
                    await timingService.EnqueueAsync(type, data, timestamp).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to handle data: {line}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle subscription or live data");
        }
    }

    private (string type, string? data, DateTimeOffset timestamp) ProcessLine(string line)
    {
        var json = JsonNode.Parse(line);
        var parts = json?["A"]?.AsArray() ?? json!.AsArray();
        var data =
            parts[1]!.GetValueKind() == JsonValueKind.Object
                ? parts[1]!.ToJsonString()
                : parts[1]!.ToString();
        return (parts[0]!.ToString(), data, DateTimeOffset.Parse(parts[2]!.ToString()));
    }
}
