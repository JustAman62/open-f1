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
    /// Defaults to <c>~/open-f1/data</c>
    /// </summary>
    public string DataDirectory { get; set; } =
        Path.Join(BaseDirectory, "data");

    /// <summary>
    /// Whether the app should expose an API at http://localhost:61937.
    /// Defaults to <c>false</c>
    /// </summary>
    public bool ApiEnabled { get; set; } = false;

    /// <summary>
    /// Whether verbose logging should be enabled.
    /// Defaults to <c>false</c>
    /// </summary>
    public bool Verbose { get; set; } = false;
}
