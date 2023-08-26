namespace OpenF1.Data;

public record RawTimingDataPoint
{
    public long Id { get; set; }
    public string EventData { get; set; } = null!;
    public DateTime LoggedDateTime { get; set; }
}

