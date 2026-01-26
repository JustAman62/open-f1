using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public sealed class LiveTimingClient : ILiveTimingClient, IDisposable
{
    private bool _disposedValue;
    private string _sessionKey = "UnknownSession";

    private readonly JsonSerializerOptions _prettyJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = TimingDataSerializerContext.Default,
    };

    private static readonly string[] _topics =
    [
        "Heartbeat",
        "ExtrapolatedClock",
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
        "TeamRadio",
        // Only available with subscription
        "CarData.z",
        "Position.z",
        "ChampionshipPrediction",
        "PitLaneTimeCollection",
        // Only available after a session?
        "PitStopSeries",
    ];
    private readonly ITimingService _timingService;
    private readonly Formula1Account _formula1Account;
    private readonly ILoggerProvider _loggerProvider;
    private readonly IOptionsMonitor<LiveTimingOptions> _options;
    private readonly ILogger<LiveTimingClient> _logger;

    public LiveTimingClient(
        ITimingService timingService,
        Formula1Account formula1Account,
        ILoggerProvider loggerProvider,
        IOptionsMonitor<LiveTimingOptions> options,
        ILogger<LiveTimingClient> logger
    )
    {
        _timingService = timingService;
        _formula1Account = formula1Account;
        _loggerProvider = loggerProvider;
        _options = options;
        _logger = logger;

        _options.OnChange(async _opts =>
        {
            if (Connection is not null)
            {
                logger.LogInformation("Options updated, restarting live timing client");
                await StartAsync();
            }
            else
            {
                _logger.LogDebug("Options updated but no live session, so not restarting.");
            }
        });
    }

    public HubConnection? Connection { get; private set; }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            _logger.LogWarning("Live timing connection already exists, restarting it");

        await DisposeConnectionAsync();

        Connection = new HubConnectionBuilder()
            .WithUrl(
                "wss://livetiming.formula1.com/signalrcore",
                configure =>
                {
                    configure.AccessTokenProvider = () =>
                        Task.FromResult(_formula1Account.AccessToken);
                }
            )
            .WithAutomaticReconnect()
            .ConfigureLogging(configure =>
                configure.AddProvider(_loggerProvider).SetMinimumLevel(LogLevel.Information)
            )
            .AddJsonProtocol(opts =>
                opts.PayloadSerializerOptions.TypeInfoResolver = TimingDataSerializerContext.Default
            )
            .Build();

        Connection.On<string, JsonNode, DateTimeOffset>("feed", HandleData);
        Connection.Closed += HandleClosedAsync;

        await Connection.StartAsync();

        _logger.LogInformation("Subscribing");
        var res = await Connection.InvokeAsync<JsonObject>("Subscribe", _topics);
        HandleSubscriptionResponse(res);

        _logger.LogInformation("Started Live Timing client");
    }

    private void HandleSubscriptionResponse(JsonObject obj)
    {
        var sessionInfo = obj?["SessionInfo"];
        var location = sessionInfo?["Meeting"]?["Location"] ?? "UnknownLocation";
        var sessionName = sessionInfo?["Name"] ?? "UnknownName";
        var year = sessionInfo?["Path"]?.ToString().Split('/')[0] ?? DateTime.Now.Year.ToString();
        _sessionKey = $"{year}_{location}_{sessionName}".Replace(' ', '_');

        _logger.LogInformation(
            "Found session key from subscription data: {SessionKey}",
            _sessionKey
        );

        var res = obj!.ToJsonString(_prettyJsonOptions);

        var filePath = Path.Join(
            _options.CurrentValue.DataDirectory,
            $"{_sessionKey}/subscribe.json"
        );
        if (!File.Exists(filePath))
        {
            var path = $"{_options.CurrentValue.DataDirectory}/{_sessionKey}";
            Directory.CreateDirectory(path);
            _logger.LogInformation("Writing subscription response to {Path}", path);
            File.WriteAllText(filePath, obj!.ToJsonString(_prettyJsonOptions));
        }
        else
        {
            _logger.LogWarning(
                "Data Subscription file at {Path} already exists, will not create a new one",
                filePath
            );
        }

        _timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string type, JsonNode json, DateTimeOffset dateTime)
    {
        var raw = new RawTimingDataPoint(type, json, dateTime);
        try
        {
            File.AppendAllText(
                Path.Join(_options.CurrentValue.DataDirectory, $"{_sessionKey}/live.jsonl"),
                JsonSerializer.Serialize(
                    raw,
                    TimingDataSerializerContext.Default.RawTimingDataPoint
                ) + Environment.NewLine
            );

            // TODO: converting `json` to a string shouldn't be needed here, we need to change the signature in TimingService
            _timingService.EnqueueAsync(type, json.ToString(), dateTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle live timing data: {Res}", raw);
        }
    }

    private async Task HandleClosedAsync(Exception? cause)
    {
        if (cause is null)
        {
            _logger.LogWarning(
                "Live timing client connection closed with no cause, so not restarting"
            );
            return;
        }

        _logger.LogWarning(cause, "Live timing client connection closed, attempting to reconnect");
        try
        {
            await DisposeConnectionAsync();
            await StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect");
        }
    }

    private async Task DisposeConnectionAsync()
    {
        if (Connection is not null)
        {
            await Connection.DisposeAsync();
        }
        Connection = null;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            _ = DisposeConnectionAsync();
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
