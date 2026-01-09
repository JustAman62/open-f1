namespace UndercutF1.Data;

public class ExtrapolatedClockProcessor(IDateTimeProvider dateTimeProvider, IMerger merger)
    : ProcessorBase<ExtrapolatedClockDataPoint>(merger)
{
    public TimeSpan ExtrapolatedRemaining()
    {
        if (Latest.Remaining.TryParseTimeSpan(out var initialRemaining))
        {
            if (Latest.Extrapolating)
            {
                var sinceStart = dateTimeProvider.Utc - Latest.Utc;
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
