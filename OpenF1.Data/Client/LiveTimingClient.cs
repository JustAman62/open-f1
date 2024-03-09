using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace OpenF1.Data;

public sealed class LiveTimingClient(
    IEnumerable<IProcessor> processors,
    ILogger<LiveTimingClient> logger
) : TimingClient(processors, logger), ILiveTimingClient, IDisposable
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

    public async Task StartAsync()
    {
        Logger.LogInformation("Starting Live Timing client");

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
            Logger.LogError(ex, "Error in live timing client: {}", ex.ToString());
        Connection.Reconnecting += () => Logger.LogWarning("Live timing client is reconnecting");
        Connection.Received += HandleData;

        var proxy = Connection.CreateHubProxy("Streaming");

        await Connection.Start();

        Logger.LogInformation("Subscribing to lots of topics");

        await proxy.Invoke(
            "Subscribe",
            (string a) => Console.WriteLine("SessionInfo " + a),
            new[] { new[] { "SessionInfo" } }
        );

        var res = await proxy.Invoke<JObject>("Subscribe", new[] { _topics });
        HandleSubscriptionResponse(res.ToString());
        Logger.LogInformation("Started Live Timing client");
    }

    private void HandleSubscriptionResponse(string res)
    {
        File.WriteAllText("./SimulationData/SubscriptionResponseTest.txt", res);

        var obj = JsonNode.Parse(res)?.AsObject();
        if (obj is null)
            return;

        ProcessData("Heartbeat", obj["Heartbeat"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("DriverList", obj["DriverList"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("TrackStatus", obj["TrackStatus"]?.ToString(), DateTimeOffset.UtcNow);
        ProcessData("LapCount", obj["LapCount"]?.ToString(), DateTimeOffset.UtcNow);

        var linesToProcess = obj["TimingData"]?["Lines"]?.AsObject() ?? [];
        foreach (var (_, line) in linesToProcess)
        {
            if (line?["Sectors"] is null)
                continue;
            line["Sectors"] = ArrayToIndexedDictionary(line["Sectors"]!);
        }
        ProcessData("TimingData", obj["TimingData"]?.ToString(), DateTimeOffset.UtcNow);

        var raceControlMessages = obj["RaceControlMessages"]?["Messages"];
        if (raceControlMessages is not null)
        {
            obj["RaceControlMessages"]!["Messages"] = ArrayToIndexedDictionary(raceControlMessages);
        }
        ProcessData(
            "RaceControlMessages",
            obj["RaceControlMessages"]?.ToString(),
            DateTimeOffset.UtcNow
        );
    }

    private JsonNode ArrayToIndexedDictionary(JsonNode node)
    {
        var dict = node.AsArray()
            .Select((val, idx) => (idx, val))
            .ToDictionary(x => x.idx.ToString(), x => x.val);
        return JsonSerializer.SerializeToNode(dict)!;
    }

    private void HandleData(string res) =>
        File.AppendAllText("./SimulationData/HandleDataTest.txt", res);

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
        }
        GC.SuppressFinalize(this);
    }
}
