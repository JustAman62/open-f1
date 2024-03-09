namespace OpenF1.Data;

/// <summary>
/// Sample: { "CurrentLap": 3, "TotalLaps": 71, "_kf": true }
/// </summary>
public sealed class LapCountDataPoint
{
    public int? CurrentLap { get; set; }
    public int? TotalLaps { get; set; }
}
