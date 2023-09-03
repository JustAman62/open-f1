using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class TimingDataProcessor : IProcessor
{
    private readonly ILiveTimingProvider _liveTimingProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<TimingDataProcessor> _logger;

    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingData;

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    /// <summary>
    /// Dictionary of { DriverNumber, { Lap, Data } }.
    /// </summary>
    public ConcurrentDictionary<string, ConcurrentDictionary<int, TimingDataPoint.TimingData.Driver>> DriverLapData { get; } = new();

    public TimingDataProcessor(ILiveTimingProvider liveTimingProvider, IMapper mapper, ILogger<TimingDataProcessor> logger)
    {
        _liveTimingProvider = liveTimingProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public Task StartAsync()
    {
        _liveTimingProvider.TimingDataReceived += Process;
        _logger.LogInformation("Subscribed to Timing Data stream for processing");
        return Task.CompletedTask;
    }

    private void Process(object? _, TimingDataPoint dataPoint)
    {
        foreach (var (driverNumber, data) in dataPoint.Data.Lines)
        {
            UpdateDriverData(driverNumber, data);
        }

        if (LatestLiveTimingDataPoint is null)
        {
            LatestLiveTimingDataPoint = _mapper.Map<TimingDataPoint>(dataPoint);
        }
        else
        {
            _mapper.Map(dataPoint, LatestLiveTimingDataPoint);
        }
    }

    private void UpdateDriverData(string driverNumber, TimingDataPoint.TimingData.Driver update)
    {
        var current = DriverLapData.GetOrAdd(driverNumber, (_) =>
        {
            ConcurrentDictionary<int, TimingDataPoint.TimingData.Driver> newData = new();
            newData.TryAdd(0, update);
            return newData;
        });

        // Check if this is a lap change. If so, we need to start a new lap
        if (update.NumberOfLaps.HasValue && !current.ContainsKey(update.NumberOfLaps.Value))
        {
            current.TryAdd(update.NumberOfLaps.Value, update);
            return;
        }

        // Existing lap, so fetch it and update it
        var latestLap = current.OrderByDescending(x => x.Key).First();
        _mapper.Map(update, latestLap.Value);
    }
}

