using AutoMapper;

namespace OpenF1.Data;

public class ExtrapolatedClockProcessor(IDateTimeProvider dateTimeProvider, IMapper mapper)
    : ProcessorBase<ExtrapolatedClockDataPoint>(mapper)
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
