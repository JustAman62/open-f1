using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        collection
            .AddSingleton<IProcessor>(x => x.GetRequiredService<TimingDataProcessor>())
            .AddSingleton<IProcessor<TimingDataPoint>>(x => x.GetRequiredService<TimingDataProcessor>())
            .AddSingleton<TimingDataProcessor>()
            .AddSingleton<IProcessor>(x => x.GetRequiredService<RaceControlMessageProcessor>())
            .AddSingleton<IProcessor<RaceControlMessageDataPoint>>(x => x.GetRequiredService<RaceControlMessageProcessor>())
            .AddSingleton<RaceControlMessageProcessor>()
            .AddSingleton<IProcessor>(x => x.GetRequiredService<HeartbeatProcessor>())
            .AddSingleton<IProcessor<HeartbeatDataPoint>>(x => x.GetRequiredService<HeartbeatProcessor>())
            .AddSingleton<HeartbeatProcessor>();

        return collection;
    }
}
