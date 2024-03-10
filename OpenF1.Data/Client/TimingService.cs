using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public class TimingService(IEnumerable<IProcessor> processors, ILogger<TimingService> logger)
    : ITimingService
{
    private CancellationTokenSource _cts = new();
    private Task? _executeTask;
    private ConcurrentQueue<(string type, string? data, DateTimeOffset timestamp)> _workItems = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip,
            AllowTrailingCommas = true,
        };

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    public ILogger Logger { get; } = logger;

    public Task StartAsync()
    {
        _cts.Cancel();
        _cts = new();
        _executeTask = Task.Run(() => ExecuteAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public virtual Task StopAsync()
    {
        _cts.Cancel();
        _executeTask = null;
        return Task.CompletedTask;
    }

    public void Enqueue(string type, string? data, DateTimeOffset timestamp) =>
        _workItems.Enqueue((type, data, timestamp));

    public List<(string type, string? data, DateTimeOffset timestamp)> GetQueueSnapshot() =>
        _workItems.ToList();

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_workItems.TryDequeue(out var res))
            {
                // Add a delay to the message timestamp,
                // then figure out how long we have to wait for to be the wall clock time
                var timestampWithDelay = res.timestamp + Delay;
                var timeToWait = DateTimeOffset.UtcNow - timestampWithDelay;
                if (timeToWait > TimeSpan.Zero)
                {
                    await Task.Delay(timeToWait).ConfigureAwait(false);
                }
                ProcessData(res.type, res.data, res.timestamp);
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
        Logger.LogInformation($"Processing {type} data point for timestamp {timestamp} :: {data}");
        if (data is null)
            return;

        switch (type)
        {
            case "Heartbeat":
                SendToProcessor<HeartbeatDataPoint>(data);
                break;
            case "RaceControlMessages":
                SendToProcessor<RaceControlMessageDataPoint>(data);
                break;
            case "TimingData":
                SendToProcessor<TimingDataPoint>(data);
                break;
            case "TimingAppData":
                SendToProcessor<TimingAppDataPoint>(data);
                break;
            case "DriverList":
                SendToProcessor<DriverListDataPoint>(data);
                break;
            case "TrackStatus":
                SendToProcessor<TrackStatusDataPoint>(data);
                break;
            case "LapCount":
                SendToProcessor<LapCountDataPoint>(data);
                break;
        }
    }

    private void SendToProcessor<T>(string data)
    {
        var json = JsonNode.Parse(data);
        if (json is null)
            return;

        // Remove the _kf property, it's not needed and breaks deserialization
        json["_kf"] = null;

        var model = json.Deserialize<T>(_jsonSerializerOptions)!;
        processors.OfType<IProcessor<T>>().ToList().ForEach(x => x.Process(model));
    }

    private JsonNode ArrayToIndexedDictionary(JsonNode node)
    {
        var dict = node.AsArray()
            .Select((val, idx) => (idx, val))
            .ToDictionary(x => x.idx.ToString(), x => x.val);
        return JsonSerializer.SerializeToNode(dict)!;
    }
}
