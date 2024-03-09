using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public abstract class TimingClient(IEnumerable<IProcessor> processors, ILogger logger)
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip,
            AllowTrailingCommas = true,
        };

    public ILogger Logger { get; } = logger;

    protected void ProcessData(string type, string? data, DateTimeOffset timestamp)
    {
        Logger.LogInformation($"Processing {type} data point for timestamp {timestamp} :: {data}");
        if (data is null) return;
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
        if (json is null) return;

        // Remove the _kf property, it's not needed and breaks deserialization
        json["_kf"] = null;

        var model = json.Deserialize<T>(_jsonSerializerOptions)!;
        processors.OfType<IProcessor<T>>().ToList().ForEach(x => x.Process(model));
    }
}
