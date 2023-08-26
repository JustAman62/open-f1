using OpenF1.Data;

public class HeartbeatDataPoint : LiveTimingDataPoint
{
    public override LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Heartbeat;
}
