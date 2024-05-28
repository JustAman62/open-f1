using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class PositionDataPointConfiguration : Profile
{
    public PositionDataPointConfiguration()
    {
        CreateMap<PositionDataPoint, PositionDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
        CreateMap<PositionDataPoint.PositionData, PositionDataPoint.PositionData>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
        CreateMap<PositionDataPoint.PositionData.Entry, PositionDataPoint.PositionData.Entry>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, PositionDataPoint.PositionData.Entry>,
            Dictionary<string, PositionDataPoint.PositionData.Entry>
        >()
            .ConvertUsingDictionaryMerge();
    }
}
