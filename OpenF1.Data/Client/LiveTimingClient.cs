using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace OpenF1.Data;

public sealed class LiveTimingClient(ITimingService timingService, ILogger<LiveTimingClient> logger)
    : ILiveTimingClient,
        IDisposable
{
    private readonly string[] _topics =
    [
        "Heartbeat",
        // "CarData.z",
        // "Position.z",
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
        "TimingData"
    ];

    private bool _disposedValue;

    public HubConnection? Connection { get; private set; }

    public Queue<string> RecentDataPoints { get; } = new();

    private string _sessionKey = "UnknownSession";

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            throw new InvalidOperationException("Timing client already has an open connection.");

        // Fetch the cookie value that is hardcoded below
        // Leaving this here to show how I did it. I don't think I need to change the value yet.
        //var httpClient = new HttpClient();
        //var res = await httpClient.GetAsync("https://livetiming.formula1.com/signalr/negotiate");
        //Console.WriteLine(res.Headers.First(x => x.Key == "Set-Cookie"));

        DisposeConnection();
        Connection = new HubConnection("https://livetiming.formula1.com");
        // Headers taken from the Fast F1 implementation
        Connection.Headers.Add("User-agent", "BestHTTP");
        Connection.Headers.Add("Accept-Encoding", "gzip, identity");
        Connection.Headers.Add("Connection", "keep-alive, Upgrade");
        Connection.Headers.Add("Cookie", "GCLB=CJulhoyzt5qwpgE;");

        Connection.EnsureReconnecting();

        Connection.Error += (ex) =>
            logger.LogError(ex, "Error in live timing client: {}", ex.ToString());
        Connection.Reconnecting += () => logger.LogWarning("Live timing client is reconnecting");
        Connection.Received += HandleData;

        var proxy = Connection.CreateHubProxy("Streaming");

        await Connection.Start();

        logger.LogInformation("Subscribing to lots of topics");

        await proxy.Invoke(
            "Subscribe",
            (string a) => Console.WriteLine("SessionInfo " + a),
            new[] { new[] { "SessionInfo" } }
        );

        var res = await proxy.Invoke<JObject>("Subscribe", new[] { _topics });
        HandleSubscriptionResponse(res.ToString());
        await timingService.StartAsync();
        logger.LogInformation("Started Live Timing client");
    }

    private void HandleSubscriptionResponse(string res)
    {
        var obj = JsonNode.Parse(res)?.AsObject();
        var sessionInfo = obj?["SessionInfo"];
        var location = sessionInfo?["Meeting"]?["Location"] ?? "UnknownLocation";
        var sessionName = sessionInfo?["Name"] ?? "UnknownName";
        _sessionKey = $"{location}_{sessionName}";

        logger.LogInformation($"Found session key from subscription data: {_sessionKey}");

        File.WriteAllText($"./SimulationData/{_sessionKey}/subscribe.txt", res);

        timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string res)
    {
        try
        {
            File.AppendAllText(
                $"./SimulationData/{_sessionKey}/live.txt",
                res.ReplaceLineEndings(string.Empty) + Environment.NewLine
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
        Connection?.Dispose();
        Connection = null;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            DisposeConnection();
            _disposedValue = true;
            timingService.StopAsync();
        }
        GC.SuppressFinalize(this);
    }
}
