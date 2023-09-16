using Microsoft.EntityFrameworkCore;

namespace OpenF1.Data;

public class LiveTimingDbContext : DbContext
{
    public LiveTimingDbContext(DbContextOptions options)
        : base(options)
    { }

    public DbSet<RawTimingDataPoint> RawTimingDataPoints => Set<RawTimingDataPoint>();

    public DbSet<DriverLap> DriverLaps => Set<DriverLap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var rawTimingDataPoint = modelBuilder.Entity<RawTimingDataPoint>();
        rawTimingDataPoint.ToTable("RAW_DATA_POINTS");
        rawTimingDataPoint.HasKey(x => x.Id);
        rawTimingDataPoint.Property(x => x.Id).HasColumnName("ID").HasColumnType("INTEGER").ValueGeneratedOnAdd();
        rawTimingDataPoint.Property(x => x.EventType).HasColumnName("EVENT_TYPE").HasColumnType("TEXT");
        rawTimingDataPoint.Property(x => x.EventData).HasColumnName("EVENT_DATA").HasColumnType("TEXT");
        rawTimingDataPoint.Property(x => x.LoggedDateTime).HasColumnName("LOGD_TS");

        var driverLap = modelBuilder.Entity<DriverLap>();
        driverLap.ToTable("DRIVER_LAPS");
        driverLap.HasKey(x => new { x.DriverNumber, x.NumberOfLaps, x.SessionName });
        base.OnModelCreating(modelBuilder);
    }
}
