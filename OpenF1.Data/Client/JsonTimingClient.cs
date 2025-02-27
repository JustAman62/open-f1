using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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

    private async Task<(
        string Location,
        DateOnly Date,
        string Session,
        string Directory
    )?> ReadSessionInfoFromDirectoryAsync(string directory)
    {
        try
        {
            var subscriptionData = await File.ReadAllTextAsync(
                    Path.Join(directory, "/subscribe.txt")
                )
                .ConfigureAwait(false);
            var sessionInfo = JsonNode
                .Parse(subscriptionData)
                ?["SessionInfo"]?.Deserialize<SessionInfoDataPoint>();

            var dateString = sessionInfo!.Path!.Split('/')[1].Split('_')[0];

            return (
                sessionInfo!.Meeting!.Circuit!.ShortName!,
                DateOnly.Parse(dateString),
                sessionInfo!.Name!,
                directory
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read session info from {Directory}", directory);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<
        Dictionary<(string Location, DateOnly Date), List<(string Session, string Directory)>>
    > GetDirectoryNamesAsync()
    {
        Directory.CreateDirectory(options.Value.DataDirectory);
        var infoTasks = Directory
            .GetDirectories(options.Value.DataDirectory)
            .Where(x =>
                Directory
                    .GetFiles(x)
                    .Any(x => x.EndsWith("live.txt", StringComparison.OrdinalIgnoreCase))
                && Directory
                    .GetFiles(x)
                    .Any(x => x.EndsWith("subscribe.txt", StringComparison.OrdinalIgnoreCase))
            )
            .Select(ReadSessionInfoFromDirectoryAsync);

        var infos = await Task.WhenAll(infoTasks);
        return infos
            .Where(x => x.HasValue)
            .GroupBy(x => (x!.Value.Location, x.Value.Date))
            .OrderByDescending(x => x.Key.Date)
            .ToDictionary(
                x => x.Key,
                x => x.Select(x => (x!.Value.Session, x.Value.Directory)).ToList()
            );
    }

    /// <inheritdoc />
    public Task StartAsync(string directory, CancellationToken cancellationToken)
    {
        ExecuteTask = LoadSimulationDataAsync(directory, cancellationToken);
        return ExecuteTask;
    }

    private async Task LoadSimulationDataAsync(
        string directory,
        CancellationToken cancellationToken
    )
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

            dateTimeProvider.Delay = CalculateDelay(subscriptionData);

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

    private TimeSpan CalculateDelay(string? subscriptionData)
    {
        // Try to start at the session start, if available. Otherwise, start at the first heartbeat.
        var sessionInfo = JsonNode
            .Parse(subscriptionData ?? string.Empty)
            ?["SessionInfo"]?.Deserialize<SessionInfoDataPoint>();

        var sessionStart =
            sessionInfo?.GetStartDateTimeUtc().GetValueOrDefault() ?? DateTime.MinValue;

        var heartbeat = JsonNode
            .Parse(subscriptionData ?? string.Empty)
            ?["Heartbeat"]?.Deserialize<HeartbeatDataPoint>();
        var firstHeartbeatDateTime = heartbeat?.Utc.DateTime ?? DateTime.MinValue;

        var delay =
            sessionStart > firstHeartbeatDateTime
                ? DateTimeOffset.UtcNow - sessionStart
                : DateTimeOffset.UtcNow - firstHeartbeatDateTime;

        logger.LogInformation(
            "Calculated a delay of {Delay} from the highest of (SessionStart: {SessionStart:s}, FirstHeartbeat: {FirstHeartbeat:s})",
            delay,
            sessionStart,
            firstHeartbeatDateTime
        );

        return delay;
    }

    private (string type, string? data, DateTimeOffset timestamp) ProcessLine(string line)
    {
        var json = JsonNode.Parse(line);
        // When we used the old ASP.NET SignalR, we received messages in an older format
        // Newer ASP.NET SignalR session recording saved RawTimingDataPoints instead
        if (json?["A"] is not null)
        {
            var parts = json["A"]!.AsArray();
            var data =
                parts[1]!.GetValueKind() == JsonValueKind.Object
                    ? parts[1]!.ToJsonString()
                    : parts[1]!.ToString();
            return (parts[0]!.ToString(), data, DateTimeOffset.Parse(parts[2]!.ToString()));
        }
        else
        {
            var parts = json.Deserialize<RawTimingDataPoint>();
            return (parts.Type, parts.Json.ToString(), parts.DateTime);
        }
    }
}
