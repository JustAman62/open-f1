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
    private readonly string[] _topics =
    [
        "Heartbeat",
        "CarData.z",
        "Position.z",
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

    public Queue<string> RecentDataPoints { get; } = new();

    private string _sessionKey = "UnknownSession";

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            logger.LogWarning("Live timing connection already exists, restarting it");

        DisposeConnection();
        Connection = new HubConnectionBuilder()
            .WithUrl("https://livetiming.formula1.com/signalrcore")
            .ConfigureLogging(x => x.AddProvider(loggerProvider))
            .AddJsonProtocol()
            .Build();

        Connection.On<string>("feed", HandleData);
        Connection.On<string, JsonObject, DateTime>("feed", (a, b, c) => logger.LogInformation("received {A}, {B}, {C}", a, b, c));

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
            Directory.CreateDirectory($"{options.Value.DataDirectory}/{_sessionKey}");
            File.WriteAllText(filePath, res);
        }
        else
        {
            logger.LogError("Data Subscription file already exists, will not create a new one");
        }

        timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string res)
    {
        logger.LogInformation("Handling data {}", res);
        try
        {
            // Remove line endings and indents to optimise the size of the string when saved to file
            res = res.ReplaceLineEndings(string.Empty).Replace("    ", string.Empty);

            File.AppendAllText(
                Path.Join(options.Value.DataDirectory, $"{_sessionKey}/live.txt"),
                res + Environment.NewLine
            );

            RecentDataPoints.Enqueue(res);
            if (RecentDataPoints.Count > 5)
                RecentDataPoints.Dequeue();

            var json = JsonNode.Parse(res);
            var data = json?["A"];

            if (data is null)
                return;

            if (data.AsArray().Count != 3)
                return;

            var eventData = data[1] is JsonValue ? data[1]!.ToString() : data[1]!.ToJsonString();

            timingService.EnqueueAsync(
                data[0]!.ToString(),
                eventData,
                DateTimeOffset.Parse(data[2]!.ToString())
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle live timing data: {res}", res);
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
