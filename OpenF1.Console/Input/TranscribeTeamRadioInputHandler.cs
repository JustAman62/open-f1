using OpenF1.Data;

namespace OpenF1.Console;

public sealed class TranscribeTeamRadioInputHandler(
    State state,
    TeamRadioProcessor teamRadio,
    ILogger<TranscribeTeamRadioInputHandler> logger
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.TeamRadio];

    public ConsoleKey[] Keys => [ConsoleKey.T];

    public string Description => _task switch
    {
        null or { IsCompletedSuccessfully: true } => "Transcribe",
        { IsCompleted: false } => "Transcribing...",
        _ => "Transcribe (Errored)"
    };

    public int Sort => 41;

    private Task? _task;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        switch (_task)
        {
            case { IsCompleted: false }:
                logger.LogInformation("Asked to start transcribing, but already working");
                break;
            default:
                _task = Task.Run(() => TranscribeAsync(state.CursorOffset));
                break;
        }
        return Task.CompletedTask;
    }

    private async Task TranscribeAsync(int offset)
    {
        try
        {
            var radio = teamRadio.Ordered.ElementAtOrDefault(offset);
            await teamRadio.TranscribeAsync(radio.Key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to transcribe");
        }
    }
}
