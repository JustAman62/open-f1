using System.Text.Json;
using OpenF1.Data;

public sealed record HeartbeatDataPoint : LiveTimingDataPoint
{
    public override LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Heartbeat;

    public HeartbeatDataPoint(string eventData, DateTime loggedDateTime)
        : base(loggedDateTime) =>
        Data = JsonSerializer.Deserialize<HeartbeatData>(eventData)!;

    public HeartbeatData Data { get; init; }

    public sealed record HeartbeatData
    {
        public DateTimeOffset Utc { get; init; }
    }
}
