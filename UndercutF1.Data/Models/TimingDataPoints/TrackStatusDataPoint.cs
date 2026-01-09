namespace UndercutF1.Data;

/// <summary>
/// Sample: {"Status": "2", "Message": "Yellow", "_kf": true}
/// </summary>
[Mergeable]
public sealed partial record TrackStatusDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TrackStatus;

    public string? Status { get; set; }
    public string? Message { get; set; }
}
