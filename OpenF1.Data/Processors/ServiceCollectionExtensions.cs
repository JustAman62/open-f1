using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        collection
            .AddProcessor<TrackStatusDataPoint, TrackStatusProcessor>()
            .AddProcessor<HeartbeatDataPoint, HeartbeatProcessor>()
            .AddProcessor<RaceControlMessageDataPoint, RaceControlMessageProcessor>()
            .AddProcessor<TimingDataPoint, TimingDataProcessor>()
            .AddProcessor<TimingAppDataPoint, TimingAppDataProcessor>()
            .AddProcessor<LapCountDataPoint, LapCountProcessor>()
            .AddProcessor<WeatherDataPoint, WeatherProcessor>()
            .AddProcessor<DriverListDataPoint, DriverListProcessor>();
            
        return collection;
    }

    private static IServiceCollection AddProcessor<TDataPoint, TProcessor>(
        this IServiceCollection services
    ) where TProcessor: class, IProcessor<TDataPoint> =>
        services
            .AddSingleton<IProcessor>(x => x.GetRequiredService<TProcessor>())
            .AddSingleton<IProcessor<TDataPoint>>(x => x.GetRequiredService<TProcessor>())
            .AddSingleton<TProcessor>();
}
