namespace OpenF1.Data;

/// <summary>
/// Provides live timing data to any subscribers.
/// </summary>
public interface ILiveTimingProvider 
{
    /// <summary>
    /// Sets up the stream of data and begins pushing messages to any subscribers.
    /// </summary>
    /// <param name="dataSource">What <see cref="DataSource"/> to read data from.</param>
    void Start(DataSource dataSource = DataSource.Live);

    /// <summary>
    /// Subscribes to a particular live timing feed.
    /// Every time an event for the provided <paramref name="liveTimingDataType"/>
    /// is received, the provided <paramref name="action"/> will be called.
    /// </summary>
    /// <param name="liveTimingDataType">the <see cref="LiveTimingDataType"/> to subscribe to.</param>
    /// <param name="action">The <see cref="Action"/> to invoke when the <paramref name="liveTimingDataType"/> event is received.</param>
    void Subscribe(LiveTimingDataType liveTimingDataType, Action<LiveTimingDataPoint> action);

    /// <summary>
    /// Subscribes to the raw data feed coming from the Live Timing data source.
    /// </summary>
    /// <param name="action">The <see cref="Action"/> to invoke with the raw data.</param>
    void SubscribeRaw(Action<RawTimingDataPoint> action);
}
