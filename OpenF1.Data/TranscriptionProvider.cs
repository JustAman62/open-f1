using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net;
using Whisper.net.Ggml;

namespace OpenF1.Data;

/// <summary>
/// Uses Whisper.net to provide transcription for files.
/// Used for transcribing drivers team radio messages.
/// </summary
public class TranscriptionProvider(
    IOptions<LiveTimingOptions> options,
    ILogger<TranscriptionProvider> logger
) : ITranscriptionProvider
{
    private string ModelPath => Path.Join(options.Value.DataDirectory, "models/ggml-base.bin");

    public async Task<string> TranscribeFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        await CheckModelInitialised().ConfigureAwait(false);
        using var whisperFactory = WhisperFactory.FromPath(ModelPath);

        using var processor = whisperFactory.CreateBuilder().WithLanguage("auto").Build();

        var destFilePath = filePath + ".wav";

        // Cannot use Pipes/Stream here for the output for some reason, so have to write to/from files
        FFMpegArguments
            .FromFileInput(filePath, verifyExists: true)
            .OutputToFile(
                destFilePath,
                overwrite: false,
                options => options.WithAudioSamplingRate(16000)
            )
            .ProcessSynchronously();

        var text = string.Empty;

        using var fileStream = File.Open(destFilePath, FileMode.Open);

        await foreach (var result in processor.ProcessAsync(fileStream, cancellationToken))
        {
            text += result.Text + Environment.NewLine + Environment.NewLine;
        }

        return text;
    }

    private async Task CheckModelInitialised()
    {
        if (!File.Exists(ModelPath))
        {
            logger.LogInformation("Whisper model not found at {}, so downloading it.", ModelPath);
            Directory.CreateDirectory(Directory.GetParent(ModelPath)!.FullName);
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.BaseEn);
            using var fileWriter = File.OpenWrite(ModelPath);
            await modelStream.CopyToAsync(fileWriter);
        }
        else
        {
            logger.LogDebug("Whisper model already exists at {}.", ModelPath);
        }
    }
}
