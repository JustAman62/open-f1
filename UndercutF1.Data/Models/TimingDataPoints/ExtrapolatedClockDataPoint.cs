namespace UndercutF1.Data;

[Mergeable]
public sealed partial record ExtrapolatedClockDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.ExtrapolatedClock;

    public DateTimeOffset? Utc { get; set; }

    public string? Remaining { get; set; } = "99:00:00";

    public bool? Extrapolating { get; set; }
}
