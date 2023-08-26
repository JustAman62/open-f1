using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProvider(this IServiceCollection collection)
    {
        collection
            .AddLiveTimingDbContext()
            .AddLiveTimingClient()
            .TryAddSingleton<ILiveTimingProvider, LiveTimingProvider>();

        return collection;
    }
}
