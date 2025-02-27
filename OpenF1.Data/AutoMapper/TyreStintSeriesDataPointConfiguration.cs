using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TyreStintSeriesDataPointConfiguration : Profile
{
    public TyreStintSeriesDataPointConfiguration()
    {
        CreateMap<TyreStintSeriesDataPoint, TyreStintSeriesDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TyreStintSeriesDataPoint.Stint, TyreStintSeriesDataPoint.Stint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<
            Dictionary<string, TyreStintSeriesDataPoint.Stint>,
            Dictionary<string, TyreStintSeriesDataPoint.Stint>
        >()
            .ConvertUsingDictionaryMerge();

        CreateMap<
            Dictionary<string, Dictionary<string, TyreStintSeriesDataPoint.Stint>>,
            Dictionary<string, Dictionary<string, TyreStintSeriesDataPoint.Stint>>
        >()
            .ConvertUsingDictionaryMerge();
    }
}
