using NetCoreAudio;
using OpenF1.Data;

namespace OpenF1.Console;

public sealed class PlayTeamRadioInputHandler(State state, TeamRadioProcessor teamRadio)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.TeamRadio];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description => _player.Playing ? "⏹ Stop" : "► Play Radio";

    public int Sort => 40;

    private readonly Player _player = new Player();

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (_player.Playing)
        {
            await _player.Stop();
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
        await _player.Play(destFileName);
    }
}
