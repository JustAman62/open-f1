using AutoMapper;

namespace OpenF1.Data;

public static class MappingUtils
{
    /// <summary>
    /// Merge dictionaries in an additive way, where entries are always added/updated but never removed.
    /// </summary>
    /// <returns>The merged dictionary</returns>
    public static Dictionary<TKey, TValue> DictionaryAdditiveMergeMap<TKey, TValue>(
        Dictionary<TKey, TValue> src,
        Dictionary<TKey, TValue> dest,
        ResolutionContext ctx
    )
        where TKey : notnull
    {
        if (src is null)
            return dest;
        if (dest is null)
            return src;
        foreach (var (k, v) in src)
        {
            if (dest.TryGetValue(k, out var existing))
            {
                // Map the existing value to itself to create a copy and prevent reference based bugs
                // Bad for performance, but good to expectations of mapping
                ctx.Mapper.Map(v, existing);
            }
            else
            {
                dest.Add(ctx.Mapper.Map<TKey>(k), ctx.Mapper.Map<TValue>(v));
            }
        }
        return dest;
    }
}
