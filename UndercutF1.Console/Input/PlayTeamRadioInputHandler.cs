using UndercutF1.Console.Audio;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class PlayTeamRadioInputHandler(
    AudioPlayer audioPlayer,
    State state,
    TeamRadioProcessor teamRadio
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.TeamRadio];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description =>
        audioPlayer.Playing ? "[olive]⏹ Stop[/]"
        : audioPlayer.Errored ? "[red]Playback Error[/]"
        : "► Play Radio";

    public int Sort => 40;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (audioPlayer.Playing)
        {
            audioPlayer.Stop();
        }
        else
        {
            await PlayAsync(state.CursorOffset, cancellationToken);
        }
    }

    private async Task PlayAsync(int offset, CancellationToken cancellationToken = default)
    {
        var radio = teamRadio.Ordered.ElementAtOrDefault(offset);
        var destFileName = await teamRadio.DownloadTeamRadioToFileAsync(
            radio.Key,
            cancellationToken
        );
        audioPlayer.Play(destFileName);
    }
}
