using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenF1.Data;

public sealed class LiveTimingClient(
    ITimingService timingService,
    IOptions<LiveTimingOptions> options,
    ILoggerProvider loggerProvider,
    ILogger<LiveTimingClient> logger
) : ILiveTimingClient, IDisposable
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = false };
    private readonly string[] _topics =
    [
        "Heartbeat",
        "CarData.z",
        "Position.z",
        "CarData",
        "Position",
        "ExtrapolatedClock",
        "TopThree",
        "TimingStats",
        "TimingAppData",
        "WeatherData",
        "TrackStatus",
        "DriverList",
        "RaceControlMessages",
        "SessionInfo",
        "SessionData",
        "LapCount",
        "TimingData",
        "ChampionshipPrediction",
        "TeamRadio"
    ];

    private bool _disposedValue;

    public HubConnection? Connection { get; private set; }

    private string _sessionKey = "UnknownSession";

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            logger.LogWarning("Live timing connection already exists, restarting it");

        DisposeConnection();
        Connection = new HubConnectionBuilder()
            .WithUrl("https://livetiming.formula1.com/signalrcore")
            .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider))
            .AddJsonProtocol()
            .Build();

        Connection.On<string, JsonNode, DateTimeOffset>("feed", HandleData);

        await Connection.StartAsync();

        logger.LogInformation("Subscribing");
        var res = await Connection.InvokeAsync<JsonObject>("Subscribe", _topics);
        HandleSubscriptionResponse(res.ToString());

        logger.LogInformation("Started Live Timing client");
    }

    private void HandleSubscriptionResponse(string res)
    {
        var obj = JsonNode.Parse(res)?.AsObject();
        var sessionInfo = obj?["SessionInfo"];
        var location = sessionInfo?["Meeting"]?["Location"] ?? "UnknownLocation";
        var sessionName = sessionInfo?["Name"] ?? "UnknownName";
        _sessionKey = $"{location}_{sessionName}".Replace(' ', '_');

        logger.LogInformation($"Found session key from subscription data: {_sessionKey}");

        var filePath = Path.Join(options.Value.DataDirectory, $"{_sessionKey}/subscribe.txt");
        if (!File.Exists(filePath))
        {
            var path = $"{options.Value.DataDirectory}/{_sessionKey}";
            Directory.CreateDirectory(path);
            logger.LogInformation("Writing subscription response to {}", path);
            File.WriteAllText(filePath, res);
        }
        else
        {
            logger.LogWarning("Data Subscription file already exists, will not create a new one");
        }

        timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string type, JsonNode json, DateTimeOffset dateTime)
    {
        if (type.EndsWith(".z"))
        {
            logger.LogInformation(
                "Handling data type: {Type}, json: {Json}, date: {Date}",
                type,
                json,
                dateTime
            );
        }

        try
        {
            var raw = new RawTimingDataPoint(type, json, dateTime);
            var rawText = JsonSerializer.Serialize(raw, _jsonSerializerOptions);
            File.AppendAllText(
                Path.Join(options.Value.DataDirectory, $"{_sessionKey}/live.txt"),
                rawText + Environment.NewLine
            );

            timingService.EnqueueAsync(type, JsonSerializer.Serialize(json), dateTime);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to handle live timing data: {Type} :: {Json} :: {DateTime}",
                type,
                json,
                dateTime
            );
        }
    }

    private void DisposeConnection()
    {
        Connection?.DisposeAsync();
        Connection = null;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            DisposeConnection();
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
