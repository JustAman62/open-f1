using OpenF1.Data;

public sealed record HeartbeatDataPoint : LiveTimingDataPoint
{
    public override LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Heartbeat;

    public HeartbeatDataPoint(HeartbeatData eventData, string sessionName, DateTimeOffset loggedDateTime)
        : base(sessionName, loggedDateTime) =>
        Data = eventData;

    public HeartbeatData Data { get; init; }

    public sealed record HeartbeatData
    {
        public DateTimeOffset Utc { get; init; }
    }
}
