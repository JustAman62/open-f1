using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TimingAppDataPointConfiguration : Profile
{
    public TimingAppDataPointConfiguration()
    {
        CreateMap<TimingAppDataPoint, TimingAppDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, TimingAppDataPoint.Driver>,
            Dictionary<string, TimingAppDataPoint.Driver>
        >()
            .ConvertUsing(MappingUtils.DictionaryAdditiveMergeMap);

        CreateMap<
            Dictionary<string, TimingAppDataPoint.Driver.Stint>,
            Dictionary<string, TimingAppDataPoint.Driver.Stint>
        >()
            .ConvertUsing(MappingUtils.DictionaryAdditiveMergeMap);

        CreateMap<TimingAppDataPoint.Driver, TimingAppDataPoint.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingAppDataPoint.Driver.Stint, TimingAppDataPoint.Driver.Stint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
    }
}
