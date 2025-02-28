using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TimingStatsDataPointConfiguration : Profile
{
    public TimingStatsDataPointConfiguration()
    {
        CreateMap<TimingStatsDataPoint, TimingStatsDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, TimingStatsDataPoint.Driver>,
            Dictionary<string, TimingStatsDataPoint.Driver>
        >()
            .ConvertUsingDictionaryMerge();

        CreateMap<
            Dictionary<string, TimingStatsDataPoint.Driver.Stat>,
            Dictionary<string, TimingStatsDataPoint.Driver.Stat>
        >()
            .ConvertUsingDictionaryMerge();

        CreateMap<TimingStatsDataPoint.Driver, TimingStatsDataPoint.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingStatsDataPoint.Driver.Stat, TimingStatsDataPoint.Driver.Stat>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
    }
}
