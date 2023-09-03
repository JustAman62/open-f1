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
    /// <param name="dataSource">What <see cref="DataSource"/> to read data from.</param>
    void Start(DataSource dataSource = DataSource.Live);
}
