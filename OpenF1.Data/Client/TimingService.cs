using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public class TimingService(
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IProcessor> processors,
    ILogger<TimingService> logger
) : BackgroundService, ITimingService
{
    private ConcurrentQueue<(string type, string? data, DateTimeOffset timestamp)> _recent = new();
    private Channel<(string type, string? data, DateTimeOffset timestamp)> _workItems =
        Channel.CreateUnbounded<(string type, string? data, DateTimeOffset timestamp)>();

    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() },
        };

    public ILogger Logger { get; } = logger;

    public async Task EnqueueAsync(string type, string? data, DateTimeOffset timestamp) =>
        await _workItems.Writer.WriteAsync((type, data, timestamp));

    public List<(string type, string? data, DateTimeOffset timestamp)> GetQueueSnapshot() =>
        _recent.ToList();

    public int GetRemainingWorkItems() => _workItems.Reader.Count;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Immediately yield to ensure all the other hosted services start as expected
        await Task.Yield();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_workItems.Reader.TryPeek(out var res))
            {
                try
                {
                    // Add a delay to the message timestamp,
                    // then figure out how long we have to wait for it to be the wall clock time
                    var timestampWithDelay = res.timestamp + dateTimeProvider.Delay;
                    var timeToWait = timestampWithDelay - DateTimeOffset.UtcNow;
                    if (timeToWait > TimeSpan.Zero)
                    {
                        if (timeToWait > TimeSpan.FromSeconds(1))
                        {
                            // If we have to wait for more than a second, then wait for just a second and repeat the loop.
                            // This way if the Delay is reduced by the user, we can react to it after at most a second.
                            Logger.LogDebug("Delaying for 1 second. Current: {CurrentTime}, Target: {TargetTime}", dateTimeProvider.Utc, res.timestamp);
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                                .ConfigureAwait(false);
                            continue;
                        }

                        Logger.LogDebug("Delaying for {TimeToWait}. Current: {CurrentTime}, Target: {TargetTime}", timeToWait, dateTimeProvider.Utc, res.timestamp);
                        await Task.Delay(timeToWait, cancellationToken).ConfigureAwait(false);
                    }

                    _recent.Enqueue(res);
                    if (_recent.Count > 5)
                        _recent.TryDequeue(out _);

                    res = await _workItems.Reader.ReadAsync();
                    ProcessData(res.type, res.data, res.timestamp);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Failed to process data {res}");
                }
            }
        }
    }

    public void ProcessSubscriptionData(string res)
    {
        try
        {
            Logger.LogInformation("Handling subscription data");

            var obj = JsonNode.Parse(res)?.AsObject();
            if (obj is null)
                return;

            ProcessData("Heartbeat", obj["Heartbeat"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("DriverList", obj["DriverList"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("TrackStatus", obj["TrackStatus"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("LapCount", obj["LapCount"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("WeatherData", obj["WeatherData"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("SessionInfo", obj["SessionInfo"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("CarData.z", obj["CarData.z"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("PositionData.z", obj["PositionData.z"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("TeamRadio", obj["TeamRadio"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData(
                "ChampionshipPrediction",
                obj["ChampionshipPrediction"]?.ToString(),
                DateTimeOffset.UtcNow
            );
            ProcessData(
                "ExtrapolatedClock",
                obj["ExtrapolatedClock"]?.ToString(),
                DateTimeOffset.UtcNow
            );
            ProcessData("TimingData", obj["TimingData"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("TimingAppData", obj["TimingAppData"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData("TimingStats", obj["TimingStats"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData(
                "RaceControlMessages",
                obj["RaceControlMessages"]?.ToString(),
                DateTimeOffset.UtcNow
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse subscription data");
        }
    }

    private void ProcessData(string type, string? data, DateTimeOffset timestamp)
    {
        if (data is null)
            return;
        if (type.EndsWith(".z"))
        {
            type = type.Replace(".z", string.Empty);
            data = DecompressUtilities.InflateBase64Data(data);
        }

        Logger.LogDebug($"Processing {type} data point for timestamp {timestamp:s} :: {data}");
        if (data is null || !Enum.TryParse<LiveTimingDataType>(type, out var liveTimingDataType))
            return;

        var json = JsonNode.Parse(data);
        if (json is null)
            return;

        switch (liveTimingDataType)
        {
            case LiveTimingDataType.Heartbeat:
                SendToProcessor<HeartbeatDataPoint>(json);
                break;
            case LiveTimingDataType.RaceControlMessages:
                json["Messages"] = ArrayToIndexedDictionary(json["Messages"]);
                SendToProcessor<RaceControlMessageDataPoint>(json);
                break;
            case LiveTimingDataType.TimingStats:
                SendToProcessor<TimingStatsDataPoint>(json);
                break;
            case LiveTimingDataType.TimingData:
                var linesToProcess = json["Lines"]?.AsObject() ?? [];
                foreach (var (_, line) in linesToProcess)
                {
                    if (line?["Sectors"] is null)
                        continue;
                    line["Sectors"] = ArrayToIndexedDictionary(line["Sectors"]!);
                }
                SendToProcessor<TimingDataPoint>(json);
                break;
            case LiveTimingDataType.TimingAppData:
                // Sometimes TimingAppData doesn't start with any Stints.
                // In this case, the first time Stints are sent are as a list instead of a dictionary, so we have to clean that up before processing
                var stintLinesToProcess = json["Lines"]?.AsObject() ?? [];
                foreach (var (_, line) in stintLinesToProcess)
                {
                    // If stints are missing, or they're already a dictionary, do nothing
                    if (
                        line?["Stints"] is null
                        || line?["Stints"]?.GetValueKind() == JsonValueKind.Object
                    )
                        continue;
                    line!["Stints"] = ArrayToIndexedDictionary(line["Stints"]!);
                }
                SendToProcessor<TimingAppDataPoint>(json);
                break;
            case LiveTimingDataType.DriverList:
                SendToProcessor<DriverListDataPoint>(json);
                break;
            case LiveTimingDataType.TrackStatus:
                SendToProcessor<TrackStatusDataPoint>(json);
                break;
            case LiveTimingDataType.LapCount:
                SendToProcessor<LapCountDataPoint>(json);
                break;
            case LiveTimingDataType.WeatherData:
                SendToProcessor<WeatherDataPoint>(json);
                break;
            case LiveTimingDataType.SessionInfo:
                SendToProcessor<SessionInfoDataPoint>(json);
                break;
            case LiveTimingDataType.ExtrapolatedClock:
                SendToProcessor<ExtrapolatedClockDataPoint>(json);
                break;
            case LiveTimingDataType.CarData:
                SendToProcessor<CarDataPoint>(json);
                break;
            case LiveTimingDataType.Position:
                SendToProcessor<PositionDataPoint>(json);
                break;
            case LiveTimingDataType.TeamRadio:
                // if Captures is an array, make it an indexed dictionary instead
                json["Captures"] = ArrayToIndexedDictionary(json["Captures"]);
                SendToProcessor<TeamRadioDataPoint>(json);
                break;
            case LiveTimingDataType.ChampionshipPrediction:
                SendToProcessor<ChampionshipPredictionDataPoint>(json);
                break;
        }
    }

    private void SendToProcessor<T>(JsonNode json)
        where T : ILiveTimingDataPoint
    {
        try
        {
            // Remove the _kf property, it's not needed and breaks deserialization
            json["_kf"] = null;

            var model = json.Deserialize<T>(_jsonSerializerOptions)!;
            processors
                .OfType<IProcessor<T>>()
                .ToList()
                .ForEach(x =>
                {
                    try
                    {
                        x.Process(model);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to process data inside processor");
                    }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to send {}, data to processor: {}",
                typeof(T).Name,
                json.ToJsonString(_jsonSerializerOptions)
            );
        }
    }

    private JsonNode? ArrayToIndexedDictionary(JsonNode? node)
    {
        if (node?.GetValueKind() == JsonValueKind.Array)
        {
            var dict = node.AsArray()
                .Select((val, idx) => (idx, val))
                .ToDictionary(x => x.idx.ToString(), x => x.val);
            return JsonSerializer.SerializeToNode(dict)!;
        }
        else
        {
            return node;
        }
    }
}
