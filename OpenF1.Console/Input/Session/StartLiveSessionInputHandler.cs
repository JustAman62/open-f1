using Microsoft.AspNetCore.SignalR.Client;
using OpenF1.Console;
using OpenF1.Data;

public class StartLiveSessionInputHandler(ILiveTimingClient liveTimingClient) : IInputHandler
{
    public bool IsEnabled => liveTimingClient.Connection?.State != HubConnectionState.Connected;

    public Screen[] ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey[] Keys => [ConsoleKey.L];

    public string Description => "Start Live Session";

    public int Sort => 40;

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo) =>
        await liveTimingClient.StartAsync();
}
