using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public sealed class LiveTimingProvider : ILiveTimingProvider
{
    private readonly ILiveTimingClient _timingClient;
    private readonly IDbContextFactory<LiveTimingDbContext> _timingDbContextFactory;
    private readonly IRawDataParser _parser;
    private readonly ISessionProvider _sessionProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<LiveTimingProvider> _logger;

    private Task? _runningTask = null;
    private Queue<RawTimingDataPoint>? _queue = null;

    public event EventHandler<RawTimingDataPoint>? RawDataReceived;
    public event EventHandler<TimingDataPoint>? TimingDataReceived;

    public LiveTimingProvider(ILiveTimingClient timingClient, IDbContextFactory<LiveTimingDbContext> timingDbContext, IRawDataParser parser, ISessionProvider sessionProvider, IMapper mapper, ILogger<LiveTimingProvider> logger)
    {
        _timingClient = timingClient;
        _timingDbContextFactory = timingDbContext;
        _parser = parser;
        _sessionProvider = sessionProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public void Start() =>
        _runningTask = _runningTask?.IsCompleted ?? true
            ? _timingClient.StartAsync(HandleRawData)
            : throw new InvalidOperationException("Live Timing client already started");

    public void StartSimulatedSession(string sessionName, string simulationName) =>
        _runningTask = _runningTask?.IsCompleted ?? true
            ? StartRecordedSessionAsync(sessionName, simulationName)
            : throw new InvalidOperationException("Simulated session already started");

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

    private async Task StartRecordedSessionAsync(string sessionName, string simulationName)
    {
        var dbContext = await _timingDbContextFactory.CreateDbContextAsync();
        var records = await dbContext
            .RawTimingDataPoints
            .Where(x => x.SessionName == sessionName)
            .OrderBy(x => x.LoggedDateTime)
            .ToListAsync();
        _queue = new Queue<RawTimingDataPoint>(records);

        var lastRecord = _queue.Dequeue();
        while (lastRecord is not null)
        {
            lastRecord.SessionName = simulationName;
            HandleRawData(lastRecord);

            var newRecord = _queue.Dequeue();

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
