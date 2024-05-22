using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingClient(this IServiceCollection collection)
    {
        collection
            .AddSingleton<IDateTimeProvider, DateTimeProvider>()
            .AddSingleton<ITimingService, TimingService>()
            .AddSingleton<IJsonTimingClient, JsonTimingClient>()
            .AddSingleton<ILiveTimingClient, LiveTimingClient>()
            .AddHostedService(sp => sp.GetRequiredService<ITimingService>());

        return collection;
    }
}
