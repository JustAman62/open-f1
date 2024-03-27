using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class WeatherDataPointConfiguration : Profile
{
    public WeatherDataPointConfiguration() =>
        CreateMap<WeatherDataPoint, WeatherDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}
