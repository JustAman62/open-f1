using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        collection
            .AddSingleton<IProcessor, TimingDataProcessor>()
            .AddSingleton<IProcessor<TimingDataPoint>, TimingDataProcessor>()
            .AddSingleton<TimingDataProcessor>()
            .AddSingleton<IProcessor, HeartbeatProcessor>()
            .AddSingleton<IProcessor<HeartbeatDataPoint>, HeartbeatProcessor>()
            .AddSingleton<HeartbeatProcessor>();

        return collection;
    }
}
