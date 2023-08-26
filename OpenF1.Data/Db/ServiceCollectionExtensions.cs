using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTimingDbContext(this IServiceCollection collection)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var dbPath = Path.Join(path, "open-f1", "timing-data.db");

        collection
            .AddDbContext<LiveTimingDbContext>(builder =>
                builder
                    .UseSqlite($"Data Source=./data.db")
                    .EnableSensitiveDataLogging(true)
                    .EnableDetailedErrors(true)
                );

        return collection;
    }
}

