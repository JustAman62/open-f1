namespace UndercutF1.Data;

/// <summary>
/// Sample: { "AirTemp": "25.6", "Humidity": "62.0", "Pressure": "1013.1", "Rainfall": "0", "TrackTemp": "31.2", "WindDirection": "7", "WindSpeed": "1.2" }
/// </summary>
[Mergeable]
public sealed partial record WeatherDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.WeatherData;

    public string? AirTemp { get; set; }
    public string? Humidity { get; set; }
    public string? Pressure { get; set; }
    public string? Rainfall { get; set; }
    public string? TrackTemp { get; set; }
    public string? WindDirection { get; set; }
    public string? WindSpeed { get; set; }
}
