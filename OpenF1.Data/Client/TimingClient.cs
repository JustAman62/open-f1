using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public abstract class TimingClient(IEnumerable<IProcessor> processors, ILogger logger)
{
    public ILogger Logger { get; } = logger;

    protected void ProcessData(string type, string data, DateTimeOffset timestamp)
    {
        Logger.LogInformation($"Processing {type} data point for timestamp {timestamp} :: {data}");
        switch (type)
        {
            case "Heartbeat":
                SendToProcessor<HeartbeatDataPoint>(data);
                break;
            case "RaceControlMessages":
                SendToProcessor<RaceControlMessageDataPoint>(data);
                break;
        }
    }

    private void SendToProcessor<T>(string data)
    {
        var model = JsonSerializer.Deserialize<T>(data)!;
        processors
            .OfType<IProcessor<T>>()
            .ToList()
            .ForEach(x => x.Process(model));
    }
}
