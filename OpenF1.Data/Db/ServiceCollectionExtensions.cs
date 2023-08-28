using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingDbContext(this IServiceCollection collection)
    {
        collection
            .AddDbContext<LiveTimingDbContext>(builder =>
            {
                var dbPath = Environment.GetEnvironmentVariable("OPEN_F1_DATA_PATH");
                if (string.IsNullOrWhiteSpace(dbPath))
                {
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
                    dbPath = Path.Join(path, "open-f1", "timing-data.db");
                }

                Directory.GetParent(dbPath)?.Create();

                _ = builder
                    .UseSqlite($"Data Source={dbPath}")
                    .EnableSensitiveDataLogging(true)
                    .EnableDetailedErrors(true);
            });

        return collection;
    }
}

