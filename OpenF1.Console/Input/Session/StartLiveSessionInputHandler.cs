using Microsoft.AspNet.SignalR.Client;
using OpenF1.Console;
using OpenF1.Data;

public class StartLiveSessionInputHandler(ILiveTimingClient liveTimingClient) : IInputHandler
{
    public Screen[]? ApplicableScreens =>
        liveTimingClient.Connection?.State == ConnectionState.Connected
            ? []
            : [Screen.ManageSession];

    public ConsoleKey ConsoleKey => ConsoleKey.L;

    public string Description => "Start Live Session";

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo) =>
        await liveTimingClient.StartAsync();
}
