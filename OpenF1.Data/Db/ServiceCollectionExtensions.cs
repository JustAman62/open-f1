using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingDbContext(this IServiceCollection collection)
    {
        collection
            .AddDbContext<LiveTimingDbContext>();

        return collection;
    }
}

