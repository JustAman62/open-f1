using Microsoft.EntityFrameworkCore;

namespace OpenF1.Data;

public class LiveTimingDbContext : DbContext
{
    public LiveTimingDbContext(DbContextOptions options)
        : base(options)
    {
    }

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
}
