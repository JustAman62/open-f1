namespace UndercutF1.Data;

[Mergeable]
public sealed partial record HeartbeatDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Heartbeat;

    public DateTimeOffset Utc { get; set; }
}
