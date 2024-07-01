using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class SessionInfoDataPointConfiguration : Profile
{
    public SessionInfoDataPointConfiguration() =>
        CreateMap<SessionInfoDataPoint, SessionInfoDataPoint>()
            .ForMember(x => x.CircuitPoints, opts => opts.Ignore())
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}
