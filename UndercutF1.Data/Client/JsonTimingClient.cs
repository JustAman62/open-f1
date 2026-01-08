using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public class JsonTimingClient(
    ITimingService timingService,
    IDateTimeProvider dateTimeProvider,
    IOptions<LiveTimingOptions> options,
    ILogger<JsonTimingClient> logger
) : IJsonTimingClient
{
    private async Task<(
        string Location,
        DateOnly Date,
        string Session,
        string Directory
    )?> ReadSessionInfoFromDirectoryAsync(string directory)
    {
        try
        {
            var subscriptionFilePath = GetSubscriptionFilePath(directory);
            var subscriptionData = await File.ReadAllTextAsync(subscriptionFilePath)
                .ConfigureAwait(false);
            var sessionInfo = JsonNode
                .Parse(subscriptionData)
                ?["SessionInfo"]?.Deserialize(TimingDataSerializerContext.Raw.SessionInfoDataPoint);

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
        try
        {
            Directory.CreateDirectory(options.Value.DataDirectory);
            var infoTasks = Directory
                .GetDirectories(options.Value.DataDirectory)
                .Where(x =>
                    Directory
                        .GetFiles(x)
                        .Any(x =>
                            x.EndsWith("live.txt", StringComparison.OrdinalIgnoreCase)
                            || x.EndsWith("live.jsonl", StringComparison.OrdinalIgnoreCase)
                        )
                    && Directory
                        .GetFiles(x)
                        .Any(x =>
                            x.EndsWith("subscribe.txt", StringComparison.OrdinalIgnoreCase)
                            || x.EndsWith("subscribe.json", StringComparison.OrdinalIgnoreCase)
                        )
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
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to create/read directory at {Directory}",
                options.Value.DataDirectory
            );
            return [];
        }
    }

    public async Task LoadSimulationDataAsync(string directory, CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            // Handle the dump of data we receive at subscription time
            var subscriptionFilePath = GetSubscriptionFilePath(directory);
            var subscriptionData = await File.ReadAllTextAsync(
                    subscriptionFilePath,
                    cancellationToken
                )
                .ConfigureAwait(false);

            timingService.ProcessSubscriptionData(subscriptionData);

            dateTimeProvider.Delay = CalculateDelay(subscriptionData);

            // Handle the real-time data
            var liveFilePath = GetLiveFilePath(directory);
            var lines = File.ReadLinesAsync(liveFilePath, cancellationToken);

            await foreach (var line in lines)
            {
                try
                {
                    var (type, data, timestamp) = ProcessLine(line);
                    await timingService.EnqueueAsync(type, data, timestamp).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to handle data: {Line}", line);
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
            ?["SessionInfo"]?.Deserialize(TimingDataSerializerContext.Raw.SessionInfoDataPoint);

        var sessionStart =
            sessionInfo?.GetStartDateTimeUtc().GetValueOrDefault() ?? DateTime.MinValue;

        var heartbeat = JsonNode
            .Parse(subscriptionData ?? string.Empty)
            ?["Heartbeat"]?.Deserialize(TimingDataSerializerContext.Raw.HeartbeatDataPoint);
        var firstHeartbeatDateTime = heartbeat?.Utc.DateTime ?? DateTime.MinValue;

        var delay =
            sessionStart > firstHeartbeatDateTime
                ? DateTime.UtcNow - sessionStart
                : DateTime.UtcNow - firstHeartbeatDateTime;

        logger.LogInformation(
            "Calculated a delay of {Delay} from the highest of (SessionStart: {SessionStart:s}, FirstHeartbeat: {FirstHeartbeat:s})",
            delay,
            sessionStart,
            firstHeartbeatDateTime
        );

        return delay;
    }

    private static (string type, string? data, DateTimeOffset timestamp) ProcessLine(string line)
    {
        var json = JsonNode.Parse(line);
        // When we used the old ASP.NET SignalR, we received messages in an older format
        // Newer ASP.NETCore SignalR session recording (and data imports) saved RawTimingDataPoints instead
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
            var parts = json.Deserialize(TimingDataSerializerContext.Raw.RawTimingDataPoint);
            return (parts.Type, parts.Json.ToString(), parts.DateTime);
        }
    }

    // We used to save data as .txt files, but we then switched to storing them as .json and .jsonl
    private static string GetSubscriptionFilePath(string directory) =>
        File.Exists(Path.Join(directory, "/subscribe.json"))
            ? Path.Join(directory, "/subscribe.json")
            : Path.Join(directory, "/subscribe.txt");

    private static string GetLiveFilePath(string directory) =>
        File.Exists(Path.Join(directory, "/live.jsonl"))
            ? Path.Join(directory, "/live.jsonl")
            : Path.Join(directory, "/live.txt");
}
