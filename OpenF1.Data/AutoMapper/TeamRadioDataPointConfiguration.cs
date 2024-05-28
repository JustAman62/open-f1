using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TeamRadioDataPointConfiguration : Profile
{
    public TeamRadioDataPointConfiguration()
    {
        CreateMap<TeamRadioDataPoint, TeamRadioDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
        CreateMap<TeamRadioDataPoint.Capture, TeamRadioDataPoint.Capture>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, TeamRadioDataPoint.Capture>,
            Dictionary<string, TeamRadioDataPoint.Capture>
        >()
            .ConvertUsingDictionaryMerge();
    }
}
