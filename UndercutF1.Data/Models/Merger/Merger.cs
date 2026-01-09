using Mapster;

namespace UndercutF1.Data;

public sealed class Merger : IMerger
{
    private static readonly TypeAdapterConfig _typeAdapterConfig = CreateTypeAdapterConfig();

    private static TypeAdapterConfig CreateTypeAdapterConfig()
    {
        var cfg = new TypeAdapterConfig();
        cfg.Default.IgnoreNullValues(true);
        return cfg;
    }

    public T Merge<T>(T source, T destination) => source.Adapt(destination, _typeAdapterConfig);
}
