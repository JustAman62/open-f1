namespace OpenF1.Data;

public sealed record DriverLap 
{
    public string DriverNumber { get; set; } = null!;
    public string SessionName { get; set; } = null!;
    public string? GapToLeader { get; set; }
    public string? Interval { get; set; }
    public int? Line { get; set; }
    public bool? InPit { get; set; }
    public bool? PitOut { get; set; }
    public int? NumberOfPitStops { get; set; }
    public int? NumberOfLaps { get; set; }
    public string? LastLapTime { get; set; }
    public bool? LastLapTimeOverallFastest { get; set; }
    public bool? LastLapTimePersonalFastest { get; set; }
    public string? Sector1Time { get; set; }
    public bool? Sector1OverallFastest { get; set; }
    public bool? Sector1PersonalFastest { get; set; }
    public string? Sector2Time { get; set; }
    public bool? Sector2OverallFastest { get; set; }
    public bool? Sector2PersonalFastest { get; set; }
    public string? Sector3Time { get; set; }
    public bool? Sector3OverallFastest { get; set; }
    public bool? Sector3PersonalFastest { get; set; }
    public string? BestLapTime { get; set; }
    public int? BestLapNumber { get; set; }
}
