using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingProcessors(this IServiceCollection collection)
    {
        collection.TryAddSingleton<IProcessor, TimingDataProcessor>();

        collection.TryAddSingleton<ProcessingService>();

        return collection;
    }
}
