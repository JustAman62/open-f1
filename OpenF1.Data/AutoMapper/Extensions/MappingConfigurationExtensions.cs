using AutoMapper;

namespace OpenF1.Data;

public static class MappingConfigurationExtensions
{
    public static IMappingExpression<
        Dictionary<TKey, TValue>,
        Dictionary<TKey, TValue>
    > ConvertUsingDictionaryMerge<TKey, TValue>(
        this IMappingExpression<Dictionary<TKey, TValue>, Dictionary<TKey, TValue>> expression
    )
        where TKey : notnull
    {
        expression.ConvertUsing(DictionaryAdditiveMergeMap);
        return expression;
    }

    /// <summary>
    /// Merge dictionaries in an additive way, where entries are always added/updated but never removed.
    /// </summary>
    /// <returns>The merged dictionary</returns>
    private static Dictionary<TKey, TValue> DictionaryAdditiveMergeMap<TKey, TValue>(
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
