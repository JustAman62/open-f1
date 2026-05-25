using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class DownloadTranscriptionModelInputHandler(
    ITranscriptionProvider transcriptionProvider,
    State state,
    ILogger<DownloadTranscriptionModelInputHandler> logger
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.DownloadTranscriptionModel];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description =>
        _task switch
        {
            { IsCompleted: false } => "[olive]Downloading, Please Wait...[/]",
            null => "Download",
            _ => "[red]Error, Retry?[/]",
        };

    public int Sort => 40;

    private Task? _task = null;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        switch (_task)
        {
            case { IsCompleted: false }:
                logger.LogInformation(
                    "Asked to download transcription model, but already downloading"
                );
                break;
            default:
                _task = Task.Run(DownloadModel, cancellationToken);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task DownloadModel()
    {
        try
        {
            await transcriptionProvider.EnsureModelDownloaded();
            logger.LogInformation("Transcription model downloaded");

            state.CurrentScreen = Screen.TeamRadio;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download transcription model");
            throw;
        }
    }
}
