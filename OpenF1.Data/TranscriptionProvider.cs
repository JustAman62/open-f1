
using Microsoft.Extensions.Options;
using Whisper.net;
using Whisper.net.Ggml;

namespace OpenF1.Data;

/// <summary>
/// Uses Whisper.net to provide transcription for files.
/// Used for transcribing drivers team radio messages.
/// </summary
public class TranscriptionProvider(IOptions<LiveTimingOptions> options) : ITranscriptionProvider
{
    private string ModelPath => Path.Join(options.Value.DataDirectory, "models/ggml-base.bin");

    public async Task<string> TranscribeFromFileAsync(string filePath)
    {
        await CheckModelInitialised().ConfigureAwait(false);
        using var whisperFactory = WhisperFactory.FromPath(ModelPath);

        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .Build();

        using var fileStream = File.OpenRead(filePath);

        var text = string.Empty;

        await foreach(var result in processor.ProcessAsync(fileStream))
        {
            text += result.Text;
        }

        return text;
    }

    private async Task CheckModelInitialised()
    {
        if (!File.Exists(ModelPath))
        {
            Directory.CreateDirectory(Directory.GetParent(ModelPath)!.FullName);
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.BaseEn);
            using var fileWriter = File.OpenWrite(ModelPath);
            await modelStream.CopyToAsync(fileWriter);
        }
    }
}
