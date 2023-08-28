using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class TimingDataPointConfiguration : Profile
{
    public TimingDataPointConfiguration()
    {
        CreateMap<TimingDataPoint, TimingDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingDataPoint.TimingData, TimingDataPoint.TimingData>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<Dictionary<string, TimingDataPoint.TimingData.Driver>, Dictionary<string, TimingDataPoint.TimingData.Driver>>()
            .ConvertUsing(DictionaryAdditiveMergeMap);

        CreateMap<Dictionary<string, TimingDataPoint.TimingData.Driver.LapSectorTime>, Dictionary<string, TimingDataPoint.TimingData.Driver.LapSectorTime>>()
            .ConvertUsing(DictionaryAdditiveMergeMap);

        CreateMap<TimingDataPoint.TimingData.Driver, TimingDataPoint.TimingData.Driver>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));

        CreateMap<TimingDataPoint.TimingData.Driver.LapSectorTime, TimingDataPoint.TimingData.Driver.LapSectorTime>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
    }

    /// <summary>
    /// Merge dictionaries in an additive way, where entries are always added/updated but never removed.
    /// </summary>
    /// <returns>The merged dictionary</returns>
    public static Dictionary<TKey, TValue> DictionaryAdditiveMergeMap<TKey, TValue>(Dictionary<TKey, TValue> src, Dictionary<TKey, TValue> dest, ResolutionContext ctx) where TKey: notnull
    {
        if (src is null) return dest;
        if (dest is null) return src;
        foreach (var (k, v) in src)
        {
            if (dest.TryGetValue(k, out var existing))
            {
                ctx.Mapper.Map(v, existing);
            }
            else
            {
                dest.Add(k, v);
            }
        }
        return dest;
    }
}
