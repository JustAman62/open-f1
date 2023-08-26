using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingClient(this IServiceCollection collection)
    {
        collection
            .AddSingleton<LiveTimingClient>();

        return collection;
    }
}

