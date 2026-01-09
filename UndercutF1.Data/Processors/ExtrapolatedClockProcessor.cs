namespace UndercutF1.Data;

public class ExtrapolatedClockProcessor(IDateTimeProvider dateTimeProvider)
    : ProcessorBase<ExtrapolatedClockDataPoint>()
{
    public TimeSpan ExtrapolatedRemaining()
    {
        if (Latest.Remaining.TryParseTimeSpan(out var initialRemaining))
        {
            if (Latest.Extrapolating.GetValueOrDefault() && Latest.Utc.HasValue)
            {
                var sinceStart = dateTimeProvider.Utc - Latest.Utc.Value;
                return initialRemaining - sinceStart;
            }
            else
            {
                return initialRemaining;
            }
        }
        else
        {
            return TimeSpan.MinValue;
        }
    }
}
