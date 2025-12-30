using Microsoft.Extensions.DependencyInjection;

namespace UndercutF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        
        foreach (var (processorType, dataPointType) in ProcessorHelper.GetProcessorTypes())
        {
            collection
                .AddSingleton(typeof(IProcessor), x => x.GetRequiredService(processorType))
                .AddSingleton(
                    typeof(IProcessor<>).MakeGenericType(dataPointType),
                    x => x.GetRequiredService(processorType)
                )
                .AddSingleton(processorType, processorType);
        }

        return collection;
    }
}
