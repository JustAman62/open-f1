using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public sealed class LiveTimingProvider : ILiveTimingProvider
{
    private readonly ILiveTimingClient _timingClient;
    private readonly LiveTimingDbContext _timingDbContext;
    private readonly ILogger<LiveTimingProvider> _logger;

    private readonly ConcurrentDictionary<LiveTimingDataType, List<Action<LiveTimingDataPoint>>> _subscriptions = new();
    private readonly List<Action<string>> _rawSubscriptions = new();

    public LiveTimingProvider(ILiveTimingClient timingClient, LiveTimingDbContext timingDbContext, ILogger<LiveTimingProvider> logger) 
    {
        _timingClient = timingClient;
        _timingDbContext = timingDbContext;
        _logger = logger;
    }

    public void Start(DataSource dataSource = DataSource.Live)
    {
        switch (dataSource) {
            case DataSource.Live:
                _timingClient.StartAsync(HandleRawData);
                break;
            default:
                throw new NotImplementedException($"Support for data source {dataSource} not yet implemented!");
        }
    }

    public void Subscribe(LiveTimingDataType liveTimingDataType, Action<LiveTimingDataPoint> action)
    {
        _logger.LogInformation("Adding new subscription for {}", liveTimingDataType);

        _subscriptions
            .AddOrUpdate(
                key: liveTimingDataType,
                addValue: new() { action },
                updateValueFactory: (_, existing) =>
                {
                    existing.Add(action);
                    return existing;
                });
    }

    public void SubscribeRaw(Action<string> action)
    {
        _logger.LogInformation("Adding raw subscription");
        _rawSubscriptions.Add(action);
    }

    private void HandleRawData(string input) 
    {
        // Pass the raw data to all the raw data subscribers
        _logger.LogInformation("Found {} raw data subscribers, sending data point", _rawSubscriptions.Count);
        _rawSubscriptions.ForEach(x => x(input));

        // TODO: Parse the input
        var dataPoint = new HeartbeatDataPoint();

        _logger.LogInformation("Received a {} data point, sending to subscribers", dataPoint.LiveTimingDataType);

        // Send the parsed data to all the subscribers for this data type
        if (_subscriptions.TryGetValue(dataPoint.LiveTimingDataType, out var subscribers))
        {
            _logger.LogInformation("Found {} subscribers for {}", subscribers.Count, dataPoint.LiveTimingDataType);
            foreach (var subscriber in subscribers)
            {
                try
                {
                    subscriber.Invoke(dataPoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Live timing subscriber for {} threw an exception: {}", dataPoint.LiveTimingDataType, ex.ToString());
                }
            }
        }
    }
}
