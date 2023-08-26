namespace OpenF1.Data;

/// <summary>
/// A client which interacts with the SignalR data stream provided by F1. 
/// </summary>
public interface ILiveTimingClient
{
    /// <summary>
    /// Starts the timing client, which establishes a connection to the real F1 live timing data source.
    /// </summary>
    /// <param name="eventHandler">The <see cref="Action"/> to call for every raw data event received from F1.</param>
    Task StartAsync(Action<string> eventHandler);
}
