using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace OpenF1.Data;

public sealed class LiveTimingClient(
    ITimingService timingService,
    IOptions<LiveTimingOptions> options,
    ILogger<LiveTimingClient> logger
) : ILiveTimingClient, IDisposable
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = false };
    private bool _disposedValue;
    private string _sessionKey = "UnknownSession";

    private static readonly string[] _topics =
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

    public HubConnection? Connection { get; private set; }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            logger.LogWarning("Live timing connection already exists, restarting it");

        DisposeConnection();

        Connection = new HubConnection("http://livetiming.formula1.com/signalr")
        {
            CookieContainer = new CookieContainer(),
            TraceWriter = new LogWriter(logger),
        };

        Connection.EnsureReconnecting();

        Connection.Received += HandleData;
        Connection.Error += (ex) =>
            logger.LogError(ex, "Error in live timing client: {}", ex.ToString());
        Connection.Reconnecting += () => logger.LogWarning("Live timing client is reconnecting");

        var hub = Connection.CreateHubProxy("Streaming");
        // hub.On<string, JToken, DateTimeOffset>("feed", HandleData);

        await Connection.Start();

        logger.LogInformation("Subscribing");
        var res = await hub.Invoke<JObject>("Subscribe", new[] { _topics });
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

        logger.LogInformation(
            "Found session key from subscription data: {SessionKey}",
            _sessionKey
        );

        var filePath = Path.Join(options.Value.DataDirectory, $"{_sessionKey}/subscribe.txt");
        if (!File.Exists(filePath))
        {
            var path = $"{options.Value.DataDirectory}/{_sessionKey}";
            Directory.CreateDirectory(path);
            logger.LogInformation("Writing subscription response to {Path}", path);
            File.WriteAllText(filePath, res);
        }
        else
        {
            logger.LogWarning("Data Subscription file already exists, will not create a new one");
        }

        timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string res)
    {
        try
        {
            // Remove line endings and indents to optimise the size of the string when saved to file
            res = res.ReplaceLineEndings(string.Empty).Replace("    ", string.Empty);
            File.AppendAllText(
                Path.Join(options.Value.DataDirectory, $"{_sessionKey}/live.txt"),
                res + Environment.NewLine
            );

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
            logger.LogError(ex, "Failed to handle live timing data: {Res}", res);
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
        }
        GC.SuppressFinalize(this);
    }

    private sealed class LogWriter : TextWriter
    {
        private readonly StringBuilder _buffer;
        private readonly ILogger _logger;
        
        public override Encoding Encoding => Encoding.UTF8;

        public LogWriter(ILogger logger)
            : base(CultureInfo.InvariantCulture)
        {
            _logger = logger;
            _buffer = new StringBuilder();
        }

        public override void Write(char value)
        {
            lock (_buffer)
            {
                if (value == '\n')
                {
                    Flush();
                }
                else
                {
                    _buffer.Append(value);
                }
            }
        }

        public override void Flush()
        {
            _logger.LogDebug("SignalR Trace: {Message}", _buffer.ToString());
            _buffer.Clear();
        }
    }
}
