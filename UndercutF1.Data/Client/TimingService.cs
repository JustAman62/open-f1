using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UndercutF1.Data;

public class TimingService(
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IProcessor> processors,
    ILogger<TimingService> logger
) : BackgroundService, ITimingService
{
    private ConcurrentQueue<(string type, string? data, DateTimeOffset timestamp)> _recent = new();
    private Channel<(string type, string? data, DateTimeOffset timestamp)> _workItems =
        Channel.CreateUnbounded<(string type, string? data, DateTimeOffset timestamp)>();

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
                    var timeToWait = res.timestamp - dateTimeProvider.Utc;
                    if (timeToWait > TimeSpan.Zero)
                    {
                        if (timeToWait > TimeSpan.FromSeconds(1))
                        {
                            // If we have to wait for more than a second, then wait for just a second and repeat the loop,
                            // without dequeueing the data.
                            // This way if the Delay is reduced by the user, we can react to it after at most a second.
                            Logger.LogTrace(
                                "Delaying for more than 1 second: {TimeToWait}. Current: {CurrentTime}, Target: {TargetTime}",
                                timeToWait,
                                dateTimeProvider.Utc,
                                res.timestamp
                            );
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                                .ConfigureAwait(false);
                            continue;
                        }

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
                    Logger.LogError(ex, "Failed to process data {Data}", res);
                }
            }
            else
            {
                // No items in the queue to read. Instead of immediately checking,
                // throttle a bit to ensure we aren't wasting CPU time
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)
                    .ConfigureAwait(false);
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
            ProcessData("Position.z", obj["Position.z"]?.ToString(), DateTimeOffset.UtcNow);
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
            ProcessData("PitStopSeries", obj["PitStopSeries"]?.ToString(), DateTimeOffset.UtcNow);
            ProcessData(
                "PitLaneTimeCollection",
                obj["PitLaneTimeCollection"]?.ToString(),
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

        Logger.LogTrace(
            "Processing {Type} data point for timestamp {Timestamp:s} :: {Data}",
            type,
            timestamp,
            data
        );
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
                    line["Sectors"] = ArrayToIndexedDictionary(line["Sectors"]);

                    foreach (var (key, sector) in line["Sectors"]!.AsObject())
                    {
                        if (sector?["Segments"] is null)
                            continue;
                        sector["Segments"] = ArrayToIndexedDictionary(sector["Segments"]);
                    }
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
            case LiveTimingDataType.PitStopSeries:
                var pitStopSeriesDrivers = json["PitTimes"]?.AsObject() ?? [];
                var pitStopDriver = pitStopSeriesDrivers.Select(x => x.Key);
                foreach (var driverNumber in pitStopDriver)
                {
                    pitStopSeriesDrivers[driverNumber] = ArrayToIndexedDictionary(
                        pitStopSeriesDrivers[driverNumber]
                    );
                }
                SendToProcessor<PitStopSeriesDataPoint>(json);
                break;
            case LiveTimingDataType.PitLaneTimeCollection:
                if (json["PitTimes"] is not null)
                {
                    json["PitTimes"]!.AsObject().Remove("_deleted");
                }
                SendToProcessor<PitLaneTimeCollectionDataPoint>(json);
                break;
        }
    }

    private void SendToProcessor<T>(JsonNode json)
        where T : ILiveTimingDataPoint
    {
        try
        {
            // Remove the _kf property, it's not needed and breaks deserialization
            json.AsObject().Remove("_kf");

            var model = json.Deserialize(TimingDataSerializerContext.Raw.GetTypeInfo<T>())!;
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
                        Logger.LogError(
                            ex,
                            "Failed to send {Name}, data to processor: {ProcessorName}: {Json}",
                            typeof(T).Name,
                            x.InputType.Name,
                            json.ToJsonString(TimingDataSerializerContext.Raw.Options)
                        );
                    }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to send {Name}, data to processor: {Json}",
                typeof(T).Name,
                json.ToJsonString(TimingDataSerializerContext.Raw.Options)
            );
        }
    }

    private static JsonNode? ArrayToIndexedDictionary(JsonNode? node)
    {
        if (node?.GetValueKind() == JsonValueKind.Array)
        {
            var dict = node.AsArray()
                .Select((val, idx) => (idx, val))
                .ToDictionary(x => x.idx.ToString(), x => x.val);

            return JsonSerializer.SerializeToNode(
                dict,
                TimingDataSerializerContext.Raw.DictionaryStringJsonNode
            )!;
        }
        else
        {
            return node;
        }
    }
}
