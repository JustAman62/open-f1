using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class DriverLapConfiguration : Profile
{
    public DriverLapConfiguration() =>
        CreateMap<TimingDataPoint.TimingData.Driver, DriverLap>()
            .ForMember(dest => dest.DriverNumber, opts => opts.Ignore())
            .ForMember(dest => dest.SessionName, opts => opts.Ignore())
            .ForMember(dest => dest.Sector1Time, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("1")!.Value))
            .ForMember(dest => dest.Sector1OverallFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("1")!.OverallFastest))
            .ForMember(dest => dest.Sector1PersonalFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("1")!.PersonalFastest))
            .ForMember(dest => dest.Sector2Time, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("2")!.Value))
            .ForMember(dest => dest.Sector2OverallFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("2")!.OverallFastest))
            .ForMember(dest => dest.Sector2PersonalFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("2")!.PersonalFastest))
            .ForMember(dest => dest.Sector3Time, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("3")!.Value))
            .ForMember(dest => dest.Sector3OverallFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("3")!.OverallFastest))
            .ForMember(dest => dest.Sector3PersonalFastest, opts => opts.MapFrom(src => src.Sectors.GetValueOrDefault("3")!.PersonalFastest))
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}

