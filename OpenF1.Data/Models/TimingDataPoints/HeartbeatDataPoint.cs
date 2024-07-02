namespace OpenF1.Data;

public sealed record HeartbeatDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Heartbeat;

    public DateTimeOffset Utc { get; init; }
}
