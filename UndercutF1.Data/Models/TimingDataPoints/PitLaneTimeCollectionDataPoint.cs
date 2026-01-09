namespace UndercutF1.Data;

// {"Type":"PitLaneTimeCollection","Json":{"PitTimes":{"1":{"RacingNumber":"1","Duration":"","Lap":"5"}}},"DateTime":"2025-09-05T15:16:10.363+00:00"}
[Mergeable]
public sealed partial record PitLaneTimeCollectionDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.PitLaneTimeCollection;

    public Dictionary<string, PitTime> PitTimes { get; set; } = new();
    public Dictionary<string, List<PitTime>> PitTimesList { get; set; } = new();

    public sealed partial record PitTime
    {
        public string? Duration { get; set; }
        public string? Lap { get; set; }
    }
}
