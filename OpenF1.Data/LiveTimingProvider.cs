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
    private readonly ISessionProvider _sessionProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<LiveTimingProvider> _logger;

    public event EventHandler<RawTimingDataPoint>? RawDataReceived;
    public event EventHandler<TimingDataPoint>? TimingDataReceived;

    public LiveTimingProvider(ILiveTimingClient timingClient, LiveTimingDbContext timingDbContext, IRawDataParser parser, ISessionProvider sessionProvider, IMapper mapper, ILogger<LiveTimingProvider> logger)
    {
        _timingClient = timingClient;
        _timingDbContext = timingDbContext;
        _parser = parser;
        _sessionProvider = sessionProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    public void Start(DataSource dataSource = DataSource.Live)
    {
        switch (dataSource)
        {
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

    public void HandleRawData(string data)
    {
        // TODO; Make this code path async
        var sessionName = _sessionProvider.GetSessionName().Result;
        var rawDataPoint = RawTimingDataPoint.Parse(data, sessionName);
        if (rawDataPoint is null) return;

        HandleRawData(rawDataPoint);
    }

    public void HandleRawData(RawTimingDataPoint rawDataPoint)
    {
        // Pass the raw data to all the raw data subscribers
        RawDataReceived?.Invoke(this, rawDataPoint);

        var dataPoint = _parser.ParseRawTimingDataPoint(rawDataPoint);
        if (dataPoint is null) return;

        _logger.LogInformation("Received a {} data point, raising event", dataPoint.LiveTimingDataType);

        switch (dataPoint)
        {
            case TimingDataPoint timingDataPoint:
                TimingDataReceived?.Invoke(this, timingDataPoint);
                break;
        }
    }

    private async Task StartRecordedSessionAsync()
    {
        var sessionName = await _sessionProvider.GetSessionName();
        var records = await _timingDbContext
            .RawTimingDataPoints
            .Where(x => x.SessionName == sessionName)
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
