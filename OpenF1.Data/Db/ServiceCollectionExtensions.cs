using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingDbContext(this IServiceCollection collection)
    {
        collection
            .AddDbContextFactory<LiveTimingDbContext>(builder =>
            {
                var connectionString = Environment.GetEnvironmentVariable("OPEN_F1_DB_CONNECTION_STRING");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = "Host=localhost:51000;Database=OPEN_F1;Username=postgres;Password=changeit";
                }

                _ = builder
                    .UseNpgsql(connectionString)
                    .EnableSensitiveDataLogging(true)
                    .EnableDetailedErrors(true);
            });

        return collection;
    }
}

