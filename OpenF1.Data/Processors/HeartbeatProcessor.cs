namespace OpenF1.Data;

public class HeartbeatProcessor : IProcessor<HeartbeatDataPoint>
{
    public DateTimeOffset LastHeartbeat { get; private set; } = DateTimeOffset.MinValue;

    public void Process(HeartbeatDataPoint data) => LastHeartbeat = data.Utc;
}
