using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        collection
            .AddSingleton<IProcessor<TimingDataPoint>, TimingDataProcessor>()
            .AddSingleton<IProcessor<HeartbeatDataPoint>, HeartbeatProcessor>();

        return collection;
    }
}
