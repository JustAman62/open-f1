using NetCoreAudio;
using OpenF1.Data;

namespace OpenF1.Console;

public sealed class PlayTeamRadioInputHandler(
    State state,
    TeamRadioProcessor teamRadio
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.TeamRadio];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description => _player.Playing
        ? "⏹ Stop"
        : "► Play Radio";

    public int Sort => 40;

    private readonly Player _player = new Player();

    public async Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        if (_player.Playing)
        {
            await _player.Stop();
        }
        else
        {
            await PlayAsync(state.CursorOffset);
        }
    }

    private async Task PlayAsync(int offset)
    {
        var radio = teamRadio.Ordered.ElementAtOrDefault(offset);
        var destFileName = await teamRadio.DownloadTeamRadioToFileAsync(radio.Key);
        await _player.Play(destFileName);
    }
}
