using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionProvider(this IServiceCollection collection)
    {
        collection
            .TryAddSingleton<ISessionProvider, SessionProvider>();

        return collection;
    }
}
