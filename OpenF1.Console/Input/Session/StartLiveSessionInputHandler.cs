using Microsoft.AspNet.SignalR.Client;
using OpenF1.Console;
using OpenF1.Data;

public class StartLiveSessionInputHandler(ILiveTimingClient liveTimingClient) : IInputHandler
{
    public bool IsEnabled => liveTimingClient.Connection?.State != ConnectionState.Connected;

    public Screen[] ApplicableScreens => [Screen.ManageSession];

    public ConsoleKey ConsoleKey => ConsoleKey.L;

    public string Description => "Start Live Session";

    public int Sort => 1;

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo) =>
        await liveTimingClient.StartAsync();
}
