using Microsoft.Extensions.DependencyInjection;

namespace UndercutF1.Data;

public static class ProcessorHelper
{
    /// <summary>
    /// Gets a list of all concreate Processor types, so that they can be generically iterated on.
    /// Intended for use when registering types in e.g. a <see cref="IServiceCollection"/>
    /// </summary>
    public static IEnumerable<(Type ProcessorType, Type DataPointType)> GetProcessorTypes() =>
        typeof(IProcessor)
            .Assembly.GetTypes()
            .Where(x => x.IsClass && x.IsAssignableTo(typeof(IProcessor)) && !x.IsGenericType)
            .Select(processorType =>
            {
                var dataPointType = GetDataPointTypeForProcessorType(processorType);
                return (processorType, dataPointType);
            });

    /// <summary>
    /// Gets the <see cref="Type"/> of the data point which the provided <paramref name="processorType"/> is for
    /// </summary>
    public static Type GetDataPointTypeForProcessorType(Type processorType) =>
        processorType
            .GetInterfaces()
            .First(x =>
                x.IsGenericType
                && x.GenericTypeArguments.Length > 0
                && x.GenericTypeArguments.First().IsAssignableTo(typeof(ILiveTimingDataPoint))
            )
            .GenericTypeArguments.First();
}
