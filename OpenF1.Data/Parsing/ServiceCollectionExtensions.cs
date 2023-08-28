using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingDataParsing(this IServiceCollection services)
    {
        services
            .TryAddSingleton<IRawDataParser, RawDataParser>();

        return services;
    }
}

