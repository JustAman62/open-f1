using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public sealed class LiveTimingProvider : ILiveTimingProvider
{
    private readonly ILiveTimingClient _timingClient;
    private readonly LiveTimingDbContext _timingDbContext;
    private readonly IRawDataParser _parser;
    private readonly IMapper _mapper;
    private readonly ILogger<LiveTimingProvider> _logger;

    private readonly ConcurrentDictionary<LiveTimingDataType, List<Action<LiveTimingDataPoint>>> _subscriptions = new();
    private readonly List<Action<RawTimingDataPoint>> _rawSubscriptions = new();

    public LiveTimingProvider(ILiveTimingClient timingClient, LiveTimingDbContext timingDbContext, IRawDataParser parser, IMapper mapper, ILogger<LiveTimingProvider> logger) 
    {
        _timingClient = timingClient;
        _timingDbContext = timingDbContext;
        _parser = parser;
        _mapper = mapper;
        _logger = logger;
    }

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    public void Start(DataSource dataSource = DataSource.Live)
    {
        switch (dataSource) {
            case DataSource.Live:
                _timingClient.StartAsync(HandleRawData);
                break;
            case DataSource.Recorded:
                Task.Run(StartRecordedSessionAsync);
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

    public void SubscribeRaw(Action<RawTimingDataPoint> action)
    {
        _logger.LogInformation("Adding raw subscription");
        _rawSubscriptions.Add(action);
    }

    public void HandleRawData(string data) 
    {
        var rawDataPoint = RawTimingDataPoint.Parse(data);
        if (rawDataPoint is null) return;

        HandleRawData(rawDataPoint);
    }

    public void HandleRawData(RawTimingDataPoint rawDataPoint)
    {
        // Pass the raw data to all the raw data subscribers
        _logger.LogInformation("Found {} raw data subscribers, sending data point", _rawSubscriptions.Count);
        _rawSubscriptions.ForEach(x => x.Invoke(rawDataPoint));

        var dataPoint = _parser.ParseRawTimingDataPoint(rawDataPoint);
        if (dataPoint is null) return;

        _logger.LogInformation("Received a {} data point, sending to subscribers", dataPoint.LiveTimingDataType);

        // Update the aggregated data
        var updatedDataPoint = UpdateAggregation(dataPoint);
        if (updatedDataPoint is null) return;

        // Send the aggregated data to all the subscribers for this data type
        if (_subscriptions.TryGetValue(updatedDataPoint.LiveTimingDataType, out var subscribers))
        {
            _logger.LogInformation("Found {} subscribers for {}", subscribers.Count, updatedDataPoint.LiveTimingDataType);
            foreach (var subscriber in subscribers)
            {
                try
                {
                    subscriber.Invoke(updatedDataPoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Live timing subscriber for {} threw an exception: {}", updatedDataPoint.LiveTimingDataType, ex.ToString());
                }
            }
        }
    }

    private LiveTimingDataPoint? UpdateAggregation(LiveTimingDataPoint dataPoint)
    {
        switch (dataPoint)
        {
            case TimingDataPoint timingDataPoint:
                if (LatestLiveTimingDataPoint is null)
                {
                    LatestLiveTimingDataPoint = _mapper.Map<TimingDataPoint>(timingDataPoint);
                }
                else
                {
                    _mapper.Map(timingDataPoint, LatestLiveTimingDataPoint);
                }
                return LatestLiveTimingDataPoint;
        }

        return null;
    }

    private async Task StartRecordedSessionAsync()
    {
        var records = await _timingDbContext
            .RawTimingDataPoints
            .OrderBy(x => x.LoggedDateTime)
            .ToListAsync();
        var queue = new Queue<RawTimingDataPoint>(records);

        var lastRecord = queue.Dequeue();
        while (lastRecord is not null)
        {
            HandleRawData(lastRecord);

            var newRecord = queue.Dequeue();

            if (newRecord is not null)
            {
                var delay = newRecord.LoggedDateTime - lastRecord.LoggedDateTime;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }
            }
            lastRecord = newRecord;
        }
    }
}
