namespace OpenF1.Data;

/// <summary>
/// Sample: {"Status": "2", "Message": "Yellow", "_kf": true}
/// </summary>
public sealed record TrackStatusDataPoint
{
    public string? Status { get; set; }
    public string? Message { get; set;}
}
