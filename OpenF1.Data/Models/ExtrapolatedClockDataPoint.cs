namespace OpenF1.Data;

public sealed record ExtrapolatedClockDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.ExtrapolatedClock;

    public DateTimeOffset Utc { get; init; }

    public string Remaining { get; init; }

    public bool Extrapolating { get; init; }
}
