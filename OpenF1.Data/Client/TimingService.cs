using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public class TimingService(
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IProcessor> processors,
    ILogger<TimingService> logger
) : ITimingService
{
    private CancellationTokenSource _cts = new();
    private Task? _executeTask;
    private ConcurrentQueue<(string type, string? data, DateTimeOffset timestamp)> _recent = new();
    private Channel<(string type, string? data, DateTimeOffset timestamp)> _workItems =
        Channel.CreateUnbounded<(string type, string? data, DateTimeOffset timestamp)>();

    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip,
            AllowTrailingCommas = true,
        };

    public ILogger Logger { get; } = logger;

    public Task StartAsync()
    {
        _cts.Cancel();
        _cts = new();
        _executeTask = Task.Factory.StartNew(
            () => ExecuteAsync(_cts.Token),
            TaskCreationOptions.LongRunning
        );
        return Task.CompletedTask;
    }

    public virtual Task StopAsync()
    {
        _cts.Cancel();
        _executeTask = null;
        return Task.CompletedTask;
    }

    public async Task EnqueueAsync(string type, string? data, DateTimeOffset timestamp) =>
        await _workItems.Writer.WriteAsync((type, data, timestamp));

    public List<(string type, string? data, DateTimeOffset timestamp)> GetQueueSnapshot() =>
        _recent.ToList();

    public int GetRemainingWorkItems() => _workItems.Reader.Count;

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
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
                            Logger.LogDebug($"Delaying for 1 second");
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                                .ConfigureAwait(false);
                            continue;
                        }

                        Logger.LogDebug($"Delaying for {timeToWait}");
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
        var obj = JsonNode.Parse(res)?.AsObject();
        if (obj is null)
            return;

        ProcessData("Heartbeat", obj["Heartbeat"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("DriverList", obj["DriverList"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("TrackStatus", obj["TrackStatus"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("LapCount", obj["LapCount"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("WeatherData", obj["WeatherData"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("SessionInfo", obj["SessionInfo"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData(
            "ExtrapolatedClock",
            obj["ExtrapolatedClock"]?.ToString(),
            DateTimeOffset.UtcNow
        );

        var linesToProcess = obj["TimingData"]?["Lines"]?.AsObject() ?? [];
        foreach (var (_, line) in linesToProcess)
        {
            if (line?["Sectors"] is null)
                continue;
            line["Sectors"] = ArrayToIndexedDictionary(line["Sectors"]!);
        }
        ProcessData("TimingData", obj["TimingData"]?.ToString(), DateTimeOffset.UtcNow);

        var stintLinesToProcess = obj["TimingAppData"]?["Lines"]?.AsObject() ?? [];
        foreach (var (_, line) in stintLinesToProcess)
        {
            if (line?["Stints"] is null)
                continue;
            line["Stints"] = ArrayToIndexedDictionary(line["Stints"]!);
        }
        ProcessData("TimingAppData", obj["TimingAppData"]?.ToString(), DateTimeOffset.UtcNow);

        var raceControlMessages = obj["RaceControlMessages"]?["Messages"];
        if (raceControlMessages is not null)
        {
            obj["RaceControlMessages"]!["Messages"] = ArrayToIndexedDictionary(raceControlMessages);
        }
        ProcessData(
            "RaceControlMessages",
            obj["RaceControlMessages"]?.ToString(),
            DateTimeOffset.UtcNow
        );
    }

    private void ProcessData(string type, string? data, DateTimeOffset timestamp)
    {
        Logger.LogDebug($"Processing {type} data point for timestamp {timestamp:s} :: {data}");
        if (data is null || !Enum.TryParse<LiveTimingDataType>(type, out var liveTimingDataType))
            return;

        switch (liveTimingDataType)
        {
            case LiveTimingDataType.Heartbeat:
                SendToProcessor<HeartbeatDataPoint>(data);
                break;
            case LiveTimingDataType.RaceControlMessages:
                SendToProcessor<RaceControlMessageDataPoint>(data);
                break;
            case LiveTimingDataType.TimingData:
                SendToProcessor<TimingDataPoint>(data);
                break;
            case LiveTimingDataType.TimingAppData:
                SendToProcessor<TimingAppDataPoint>(data);
                break;
            case LiveTimingDataType.DriverList:
                SendToProcessor<DriverListDataPoint>(data);
                break;
            case LiveTimingDataType.TrackStatus:
                SendToProcessor<TrackStatusDataPoint>(data);
                break;
            case LiveTimingDataType.LapCount:
                SendToProcessor<LapCountDataPoint>(data);
                break;
            case LiveTimingDataType.WeatherData:
                SendToProcessor<WeatherDataPoint>(data);
                break;
            case LiveTimingDataType.SessionInfo:
                SendToProcessor<SessionInfoDataPoint>(data);
                break;
            case LiveTimingDataType.ExtrapolatedClock:
                SendToProcessor<ExtrapolatedClockDataPoint>(data);
                break;
        }
    }

    private void SendToProcessor<T>(string data)
    {
        try
        {
            var json = JsonNode.Parse(data);
            if (json is null)
                return;

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
            Logger.LogError(ex, "Failed to send data to processor: {}", data);
        }
    }

    private JsonNode ArrayToIndexedDictionary(JsonNode node)
    {
        var dict = node.AsArray()
            .Select((val, idx) => (idx, val))
            .ToDictionary(x => x.idx.ToString(), x => x.val);
        return JsonSerializer.SerializeToNode(dict)!;
    }
}
