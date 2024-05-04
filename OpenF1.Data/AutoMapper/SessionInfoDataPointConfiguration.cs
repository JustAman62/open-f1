using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class SessionInfoDataPointConfiguration : Profile
{
    public SessionInfoDataPointConfiguration() =>
        CreateMap<SessionInfoDataPoint, SessionInfoDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}
