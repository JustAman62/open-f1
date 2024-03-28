using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class DriverListDataPointConfiguration : Profile
{
    public DriverListDataPointConfiguration()
    {
        CreateMap<DriverListDataPoint.Driver, DriverListDataPoint.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, DriverListDataPoint.Driver>,
            Dictionary<string, DriverListDataPoint.Driver>
        >()
            .ConvertUsingDictionaryMerge();
    }
}
