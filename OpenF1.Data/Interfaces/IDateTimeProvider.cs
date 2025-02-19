namespace OpenF1.Data;

public interface IDateTimeProvider
{
    TimeSpan Delay { get; set; }

    DateTimeOffset Utc { get; }
}
