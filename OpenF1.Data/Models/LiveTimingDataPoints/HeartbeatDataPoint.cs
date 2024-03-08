using OpenF1.Data;

public sealed record HeartbeatDataPoint
{
    public DateTimeOffset Utc { get; init; }
}
