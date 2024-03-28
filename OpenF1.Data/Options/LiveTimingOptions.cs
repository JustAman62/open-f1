namespace OpenF1.Data;

/// <summary>
/// Options to configure the behaviour of live timing.
/// </summary>
public record LiveTimingOptions
{
    public static string BaseDirectory =>
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "open-f1");

    /// <summary>
    /// The directory to read and store live timing data for simulations.
    /// When live sessions are being listened to, all data received will be recorded in this directory.
    /// </summary>
    public string DataDirectory { get; set; } =
        Path.Join(BaseDirectory, "data");
}
