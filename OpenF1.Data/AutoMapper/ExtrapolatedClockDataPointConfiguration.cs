using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class ExtrapolatedClockDataPointConfiguration : Profile
{
    public ExtrapolatedClockDataPointConfiguration() =>
        CreateMap<ExtrapolatedClockDataPoint, ExtrapolatedClockDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}
