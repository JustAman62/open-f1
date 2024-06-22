namespace OpenF1.Data;

public interface ITranscriptionProvider
{
    public Task<string> TranscribeFromFileAsync(string filePath);
}
