using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class ChampionshipPredictionDataPointConfiguration : Profile
{
    public ChampionshipPredictionDataPointConfiguration()
    {
        CreateMap<ChampionshipPredictionDataPoint, ChampionshipPredictionDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
        CreateMap<ChampionshipPredictionDataPoint.Driver, ChampionshipPredictionDataPoint.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
        CreateMap<ChampionshipPredictionDataPoint.Team, ChampionshipPredictionDataPoint.Team>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, ChampionshipPredictionDataPoint.Driver>,
            Dictionary<string, ChampionshipPredictionDataPoint.Driver>
        >()
            .ConvertUsingDictionaryMerge();
        CreateMap<
            Dictionary<string, ChampionshipPredictionDataPoint.Team>,
            Dictionary<string, ChampionshipPredictionDataPoint.Team>
        >()
            .ConvertUsingDictionaryMerge();
    }
}
