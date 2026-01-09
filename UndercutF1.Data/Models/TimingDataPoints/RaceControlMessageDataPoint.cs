namespace UndercutF1.Data;

/// <summary>
/// Sample: {"Messages": {"2": {"Utc": "2021-03-27T12:00:00", "Category": "Flag", "Flag": "GREEN", "Scope": "Track", "Message": "GREEN LIGHT - PIT EXIT OPEN"}}}
/// </summary>
[Mergeable]
public sealed partial record RaceControlMessageDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.RaceControlMessages;

    public Dictionary<string, RaceControlMessage> Messages { get; set; } = new();

    public sealed partial record RaceControlMessage
    {
        public DateTimeOffset Utc { get; set; }
        public string? Message { get; set; }
    }
}
