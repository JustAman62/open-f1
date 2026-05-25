using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net;
using Whisper.net.Ggml;

namespace UndercutF1.Data;

/// <summary>
/// Uses Whisper.net to provide transcription for files.
/// Used for transcribing drivers team radio messages.
/// </summary>
public class TranscriptionProvider(
    IOptions<LiveTimingOptions> options,
    ILogger<TranscriptionProvider> logger
) : ITranscriptionProvider
{
    private const int ExpectedModelFileSize = 574_041_195;

    private Stream? _downloadStream = null;

    public string ModelPath =>
        Path.Join(options.Value.DataDirectory, "models/ggml-large-v3-turbo-q5.bin");

    public bool IsModelDownloaded => File.Exists(ModelPath);

    public double DownloadProgress => (_downloadStream?.Position ?? 0.0) / ExpectedModelFileSize;

    public async Task<string> TranscribeFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureModelDownloaded().ConfigureAwait(false);
        using var whisperFactory = WhisperFactory.FromPath(ModelPath);

        using var processor = whisperFactory.CreateBuilder().WithLanguage("auto").Build();

        var destFilePath = filePath + ".wav";

        if (!File.Exists(destFilePath))
        {
            // Cannot use Pipes/Stream here for the output for some reason, so have to write to/from files
            // Whisper requires input files to be sampled at 16kHz, so use FFMpeg to resample the team radio file
            // FFMpeg will also convert the mp2 file to a .wav file
            FFMpegArguments
                .FromFileInput(filePath, verifyExists: true)
                .OutputToFile(
                    destFilePath,
                    overwrite: false,
                    options => options.WithAudioSamplingRate(16000)
                )
                .ProcessSynchronously();
        }

        var text = string.Empty;

        using var fileStream = File.Open(destFilePath, FileMode.Open);

        await foreach (var result in processor.ProcessAsync(fileStream, cancellationToken))
        {
            text += result.Text + Environment.NewLine + Environment.NewLine;
        }

        return text;
    }

    public async Task EnsureModelDownloaded()
    {
        if (!IsModelDownloaded)
        {
            logger.LogInformation(
                "Whisper model not found at {ModelPath}, so downloading it.",
                ModelPath
            );
            Directory.CreateDirectory(Directory.GetParent(ModelPath)!.FullName);
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(
                GgmlType.LargeV3Turbo,
                QuantizationType.Q5_0
            );

            using var fileWriter = File.OpenWrite(ModelPath);

            _downloadStream = fileWriter;

            await modelStream.CopyToAsync(fileWriter);

            _downloadStream = null;
        }
        else
        {
            logger.LogDebug("Whisper model already exists at {ModelPath}.", ModelPath);
        }
    }
}
