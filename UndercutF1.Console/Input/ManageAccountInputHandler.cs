using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class ManageAccountInputHandler(
    AccountLogin accountLogin,
    ILiveTimingClient liveTimingClient,
    ILogger<ManageAccountInputHandler> logger
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageAccount];

    public ConsoleKey[] Keys => [ConsoleKey.L];

    public string Description =>
        _status switch
        {
            StatusFlags.None => "Login",
            StatusFlags.Waiting => "[olive]Waiting for browser window to be closed[/]",
            StatusFlags.TokenReceived => "[olive]Token received, please close browser window[/]",
            StatusFlags.Failed => "[red]Login failed, please try again[/]",
            _ => "[green]Login complete[/]",
        };

    public int Sort => 40;

    private StatusFlags _status = StatusFlags.None;

    private Task _loginTask = Task.CompletedTask;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (!_loginTask.IsCompleted)
            return;

        _status = StatusFlags.Waiting;

        _loginTask = accountLogin.LoginAsync(
            async (status) =>
            {
                switch (status)
                {
                    case AccountLogin.LoginStatus.TokenReceived:
                        _status = StatusFlags.TokenReceived;
                        break;
                    case AccountLogin.LoginStatus.Failed:
                        _status = StatusFlags.Failed;
                        break;
                    case AccountLogin.LoginStatus.Complete:
                        _status = StatusFlags.Complete;
                        if (
                            liveTimingClient.Connection?.State
                            == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected
                        )
                        {
                            logger.LogInformation(
                                "F1TV login (via TUI) complete, restarting live timing client with new token"
                            );
                            await liveTimingClient.StartAsync();
                        }
                        break;
                }
            }
        );
        await _loginTask;
    }

    private enum StatusFlags
    {
        None,
        Waiting,
        TokenReceived,
        Failed,
        Complete,
    }
}
