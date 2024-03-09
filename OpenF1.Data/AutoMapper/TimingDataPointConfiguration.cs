using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TimingDataPointConfiguration : Profile
{
    public TimingDataPointConfiguration()
    {
        CreateMap<TimingDataPoint, TimingDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, TimingDataPoint.Driver>,
            Dictionary<string, TimingDataPoint.Driver>
        >()
            .ConvertUsing(MappingUtils.DictionaryAdditiveMergeMap);

        CreateMap<
            Dictionary<string, TimingDataPoint.Driver.LapSectorTime>,
            Dictionary<string, TimingDataPoint.Driver.LapSectorTime>
        >()
            .ConvertUsing(MappingUtils.DictionaryAdditiveMergeMap);

        CreateMap<TimingDataPoint.Driver, TimingDataPoint.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingDataPoint.Driver.BestLap, TimingDataPoint.Driver.BestLap>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingDataPoint.Driver.LapSectorTime, TimingDataPoint.Driver.LapSectorTime>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
    }
}
