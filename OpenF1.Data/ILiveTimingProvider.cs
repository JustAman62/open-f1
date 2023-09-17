namespace OpenF1.Data;

/// <summary>
/// Provides live timing data to any subscribers.
/// </summary>
public interface ILiveTimingProvider 
{
    event EventHandler<RawTimingDataPoint>? RawDataReceived;

    event EventHandler<TimingDataPoint>? TimingDataReceived;

    /// <summary>
    /// Sets up the stream of data and begins pushing messages to any subscribers.
    /// </summary>
    void Start();

    /// <summary>
    /// Starts a simulated session for a previously recorded session.
    /// </summary>
    /// <param name="sessionName">The name of the previous recorded session.</param>
    /// <param name="simulationName">The name of the session to record any new data under..</param>
    void StartSimulatedSession(string sessionName, string simulationName);
}
