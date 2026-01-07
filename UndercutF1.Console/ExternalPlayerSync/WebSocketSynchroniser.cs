using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using UndercutF1.Data;

namespace UndercutF1.Console.ExternalPlayerSync;

public sealed class WebSocketSynchroniser(
    SessionInfoProcessor sessionInfo,
    IDateTimeProvider dateTimeProvider,
    IOptions<Console.ConsoleOptions> options,
    ILogger<WebSocketSynchroniser> logger
) : BackgroundService
{
    private ClientWebSocket _webSocket = new();

    public WebSocketState State => _webSocket.State;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!ValidateOptions())
        {
            return;
        }

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteAsyncInternal(stoppingToken);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "WebSocket Synchroniser stopping");
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "UndercutF1 stopping",
                    CancellationToken.None
                );
                return;
            }
            catch (Exception ex)
            {
                // Something failed, but try to keep the service going by waiting and reconnecting
                var interval = options.Value.ExternalPlayerSync!.WebSocketConnectInterval;
                logger.LogError(
                    ex,
                    "WebSocket Synchroniser encountered an error and is waiting for {Interval}ms before reconnecting",
                    interval
                );
                await Task.Delay(interval, stoppingToken);
            }
        }
    }

    private async Task ExecuteAsyncInternal(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Connecting to {Service} at {Url}",
            options.Value.ExternalPlayerSync!.ServiceType,
            options.Value.ExternalPlayerSync.Url
        );
        _webSocket.Dispose();
        _webSocket = new();
        await _webSocket.ConnectAsync(options.Value.ExternalPlayerSync!.Url!, stoppingToken);

        logger.LogInformation(
            "Connected to {Service} at {Url}",
            options.Value.ExternalPlayerSync!.ServiceType,
            options.Value.ExternalPlayerSync.Url
        );

        while (!stoppingToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            Array.Fill<byte>(buffer, 0);
            try
            {
                var result = await _webSocket.ReceiveAsync(buffer, stoppingToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer).TrimEnd((char)0);
                    logger.LogDebug("Received Kodi Notification: {Text}", message);

                    HandleKodiNotification(message);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Handles the Kodi notification message
    /// Example message:
    /// <code>
    /// {"jsonrpc":"2.0","method":"Player.OnPlay","params":{"data":{"item":{"title":"download.m4v","type":"movie"},"player":{"playerid":1,"speed":1}},"sender":"xbmc"}}
    /// </code>
    /// The <c>method</c> key tells us everything we need to know, so thats the only part of the message to look at.
    /// </summary>
    private void HandleKodiNotification(string message)
    {
        var json = JsonNode.Parse(
            message,
            new JsonNodeOptions { PropertyNameCaseInsensitive = true }
        );
        var method = json?["method"]?.GetValue<string>();

        switch (method)
        {
            case "Player.OnResume":
                logger.LogInformation(
                    "Received Kodi notification, ensuring timing session is playing"
                );
                SetPlaybackStatus(true);
                return;
            case "Player.OnPause":
                logger.LogInformation(
                    "Received Kodi notification, ensuring timing session is paused"
                );
                SetPlaybackStatus(false);
                return;
            default:
                logger.LogDebug(
                    "Received Kodi notification for non-supported method {Method}",
                    method
                );
                return;
        }
    }

    private void SetPlaybackStatus(bool shouldBePlaying)
    {
        if (sessionInfo.Latest.Name is null)
        {
            logger.LogDebug("No active session, so ignoring request to pause/play");
            return;
        }

        if (shouldBePlaying && dateTimeProvider.IsPaused)
        {
            dateTimeProvider.TogglePause();
            return;
        }

        if (!shouldBePlaying && !dateTimeProvider.IsPaused)
        {
            dateTimeProvider.TogglePause();
            return;
        }

        logger.LogDebug("Playback state already matches requested, no action to perform");
    }

    private bool ValidateOptions()
    {
        var syncOptions = options.Value.ExternalPlayerSync;
        if (syncOptions is null || !syncOptions.Enabled)
        {
            logger.LogDebug("External Player Sync disabled");
            return false;
        }

        if (syncOptions.Url is null)
        {
            logger.LogError(
                "External Player Sync enabled, but no URL configured, so unable to start"
            );
            return false;
        }

        if (!Enum.IsDefined(syncOptions.ServiceType))
        {
            logger.LogError(
                "External Player Sync enabled, but invalid ServiceType {ServiceType} configured, so unable to start",
                syncOptions.ServiceType
            );
            return false;
        }

        if (syncOptions.WebSocketConnectInterval < 100)
        {
            logger.LogError(
                "External Player Sync enabled, but invalid WebSocketConnectInterval {WebSocketConnectInterval} configured, so unable to start. WebSocketConnectInterval is configured in milliseconds and may not be less than 100ms",
                syncOptions.WebSocketConnectInterval
            );
            return false;
        }

        return true;
    }
}
