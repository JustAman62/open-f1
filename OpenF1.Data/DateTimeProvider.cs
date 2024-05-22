namespace OpenF1.Data;

public class DateTimeProvider : IDateTimeProvider
{
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public DateTimeOffset Utc => DateTimeOffset.UtcNow - Delay;
}
