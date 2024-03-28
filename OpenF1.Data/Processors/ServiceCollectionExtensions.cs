using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        var processorTypes = typeof(IProcessor)
            .Assembly.GetTypes()
            .Where(x => x.IsClass && x.IsAssignableTo(typeof(IProcessor)));

        foreach (var processorType in processorTypes)
        {
            var dataPointType = processorType
                .GetInterfaces()
                .First(x =>
                    x.IsGenericType
                    && x.GenericTypeArguments.Length > 0
                    && x.GenericTypeArguments.First().IsAssignableTo(typeof(ILiveTimingDataPoint))
                )
                .GenericTypeArguments.First();

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
