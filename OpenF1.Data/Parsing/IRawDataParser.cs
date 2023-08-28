namespace OpenF1.Data;

public interface IRawDataParser
{
    /// <summary>
    /// Parses the provided <paramref name="dataPoint"/> and returns a
    /// derived class of <see cref="LiveTimingDataPoint"/> representing
    /// the exact event this data point represents.
    /// </summary>
    /// <param name="dataPoint">The <see cref="RawTimingDataPoint"/> to parse.</param>
    /// <returns>A derived class of <see cref="LiveTimingDataPoint"/>, or <see langword="null"/> if parsing fails..</returns>
    LiveTimingDataPoint? ParseRawData(RawTimingDataPoint dataPoint);
}

