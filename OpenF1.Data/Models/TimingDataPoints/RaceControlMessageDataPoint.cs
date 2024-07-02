namespace OpenF1.Data;

/// <summary>
/// Sample: {"Messages": {"2": {"Utc": "2021-03-27T12:00:00", "Category": "Flag", "Flag": "GREEN", "Scope": "Track", "Message": "GREEN LIGHT - PIT EXIT OPEN"}}}
/// </summary>
public sealed record RaceControlMessageDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.RaceControlMessages;

    public Dictionary<string, RaceControlMessage> Messages { get; init; } = new();

    public sealed record RaceControlMessage 
    {
        public DateTimeOffset Utc { get; init; }
        public string Message { get; init; } = null!;
    }
}
