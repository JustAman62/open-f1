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
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
        var dbPath = Path.Join(path, "open-f1", "timing-data.db");

        _logger.LogInformation("Using Sqlite DB for timing data at path: {}", dbPath);

        Directory.GetParent(dbPath)?.Create();

        optionsBuilder
            .UseSqlite($"Data Source={dbPath}")
            .EnableSensitiveDataLogging(true)
            .EnableDetailedErrors(true);

        base.OnConfiguring(optionsBuilder);
    }
}
