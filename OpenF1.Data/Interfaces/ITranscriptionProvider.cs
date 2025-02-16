namespace OpenF1.Data;

public interface ITranscriptionProvider
{
    Task<string> TranscribeFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );
}
