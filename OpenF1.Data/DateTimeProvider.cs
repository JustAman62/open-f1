namespace OpenF1.Data;

public interface IDateTimeProvider
{
    public TimeSpan Delay { get; set; }

    public DateTimeOffset Utc { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public DateTimeOffset Utc => DateTimeOffset.UtcNow - Delay;
}
