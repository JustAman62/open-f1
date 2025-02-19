namespace OpenF1.Data;

public interface ILiveTimingDataPoint
{
    /// <summary>
    /// The <see cref="LiveTimingDataType"/> for this data point.
    /// </summary>
    LiveTimingDataType LiveTimingDataType { get; }
}
