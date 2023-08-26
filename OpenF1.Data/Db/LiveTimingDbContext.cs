using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class LiveTimingDbContext : DbContext
{
    private readonly ILogger<LiveTimingDbContext> _logger;

    public LiveTimingDbContext(DbContextOptions options, ILogger<LiveTimingDbContext> logger)
        : base(options)
        => _logger = logger;

    public DbSet<RawTimingDataPoint> RawTimingDataPoints => Set<RawTimingDataPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var rawTimingDataPoint = modelBuilder.Entity<RawTimingDataPoint>();
        rawTimingDataPoint.HasKey(x => x.Id);
        rawTimingDataPoint.Property(x => x.Id).HasColumnName("ID").HasColumnType("INTEGER").ValueGeneratedOnAdd();
        rawTimingDataPoint.Property(x => x.EventData).HasColumnName("DATA").HasColumnType("nvarchar(8000)");
        rawTimingDataPoint.Property(x => x.LoggedDateTime).HasColumnName("LOGD_TS");

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Environment.GetEnvironmentVariable("OPEN_F1_DATA_PATH");
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            dbPath = Path.Join(path, "open-f1", "timing-data.db");
            _logger.LogInformation("Using Sqlite DB for timing data at default path: {}. Customize the path using env variable OPEN_F1_DATA_PATH.", dbPath);
        }
        else
        {
            _logger.LogInformation("Using Sqlite DB for timing data at custom path (set by OPEN_F1_DATA_PATH env var): {}", dbPath);
        }

        Directory.GetParent(dbPath)?.Create();

        _ = optionsBuilder
            .UseSqlite($"Data Source={dbPath}")
            .EnableSensitiveDataLogging(true)
            .EnableDetailedErrors(true);

        base.OnConfiguring(optionsBuilder);
    }
}
