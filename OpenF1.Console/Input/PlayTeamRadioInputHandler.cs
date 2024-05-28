using System.Net;
using OpenF1.Data;

namespace OpenF1.Console;

public sealed class PlayTeamRadioInputHandler(
    State state,
    TeamRadioProcessor teamRadio,
    SessionInfoProcessor sessionInfo
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.SessionStats];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description => $"Play";

    public int Sort => 40;

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var radio = teamRadio.Ordered.ElementAtOrDefault(state.CursorOffset);
        var path = $"https://livetiming.formula1.com/static/{sessionInfo.Latest.Path}{radio.Value.Path}";
        var destFileName = Path.GetTempFileName();

        await DownloadFileAsync(path, destFileName);

        var player = new NetCoreAudio.Player();
        await player.Play(destFileName);
    }

    private async Task DownloadFileAsync(string downloadUri, string destFilePath)
    {
        // TODO: Use DI based HttpClients
        using var httpClient = new HttpClient();
        var downloadStream = await httpClient.GetStreamAsync(downloadUri).ConfigureAwait(false);
        using var fsStream = new FileStream(destFilePath, FileMode.OpenOrCreate);
        await downloadStream.CopyToAsync(fsStream);
    }
}
