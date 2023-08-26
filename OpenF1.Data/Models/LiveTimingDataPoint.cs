namespace OpenF1.Data;

/// <summary>
/// The base class for all types of Live Timing data points. T
/// he <see cref="LiveTimingDataType"/> property is used to determine 
/// what type of data point this object represents.
/// </summary>
public abstract class LiveTimingDataPoint
{
    /// <summary>
    /// The discriminator for this type, specifying which type of data point this is.
    /// </summary>
    public abstract LiveTimingDataType LiveTimingDataType { get; }
}
