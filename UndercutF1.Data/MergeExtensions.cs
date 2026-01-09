namespace UndercutF1.Data;

internal static partial class MergeExtensions
{
    /// <summary>
    /// Merge dictionaries in an additive way, where entries are always added/updated but never removed.
    /// </summary>
    /// <returns>The merged dictionary</returns>
    public static Dictionary<TKey, TValue> MergeWith<TKey, TValue>(
        this Dictionary<TKey, TValue> src,
        Dictionary<TKey, TValue>? other
    )
        where TKey : notnull
        where TValue : IMergeable<TValue>
    {
        if (other is null)
            return src;
        if (src is null)
            return other;
        foreach (var (k, v) in other)
        {
            if (src.TryGetValue(k, out var existing))
            {
                existing.MergeWith(v);
            }
            else
            {
                src.Add(k, v);
            }
        }
        return src;
    }

    public static Dictionary<TKey, Dictionary<TKeyInner, TValue>> MergeWith<
        TKey,
        TKeyInner,
        TValue
    >(
        this Dictionary<TKey, Dictionary<TKeyInner, TValue>> src,
        Dictionary<TKey, Dictionary<TKeyInner, TValue>> other
    )
        where TKey : notnull
        where TKeyInner : notnull
        where TValue : IMergeable<TValue>
    {
        if (src is null)
            return other;
        if (other is null)
            return src;
        foreach (var (k, v) in other)
        {
            if (src.TryGetValue(k, out var existing))
            {
                existing.MergeWith(v);
            }
            else
            {
                src.Add(k, v);
            }
        }
        return src;
    }

    public static Dictionary<TKey, List<TValue>> MergeWith<TKey, TValue>(
        this Dictionary<TKey, List<TValue>> src,
        Dictionary<TKey, List<TValue>> other
    )
        where TKey : notnull
        where TValue : IMergeable<TValue>
    {
        if (src is null)
            return other;
        if (other is null)
            return src;
        foreach (var (k, v) in other)
        {
            if (src.TryGetValue(k, out var existing))
            {
                existing.MergeWith(v);
            }
            else
            {
                src.Add(k, v);
            }
        }
        return src;
    }

    public static T? MergeWith<T>(this T? src, T? other)
        where T : struct, Enum => other is not null ? other : src;

    public static List<T> MergeWith<T>(this List<T> src, List<T>? other)
    {
        src ??= [];

        if (other is not null)
            src.AddRange(other);

        return src;
    }

    public static int? MergeWith(this int? src, int? other) => other is not null ? other : src;

    public static bool? MergeWith(this bool? src, bool? other) => other is not null ? other : src;

    public static bool MergeWith(this bool _, bool other) => other;

    public static string? MergeWith(this string? src, string? other) =>
        other is not null ? other : src;

    public static decimal? MergeWith(this decimal? src, decimal? other) =>
        other is not null ? other : src;

    public static DateTimeOffset MergeWith(this DateTimeOffset _, DateTimeOffset other) => other;

    public static DateTimeOffset? MergeWith(this DateTimeOffset? src, DateTimeOffset? other) =>
        other is not null ? other : src;

    public static DateTime? MergeWith(this DateTime? src, DateTime? other) =>
        other is not null ? other : src;

    public static T? MergeClasses<T>(T? src, T? other)
        where T : IMergeable<T>
    {
        if (other is null)
            return src;
        if (src is null)
            return src;
        src.MergeWith(other);
        return src;
    }
}
