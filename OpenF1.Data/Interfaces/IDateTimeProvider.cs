namespace OpenF1.Data;

public interface IDateTimeProvider
{
    public TimeSpan Delay { get; set; }

    public DateTimeOffset Utc { get; }
}
