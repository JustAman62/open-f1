namespace UndercutF1.Data;

[Mergeable]
public sealed partial record PitStopSeriesDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.PitStopSeries;

    public Dictionary<string, Dictionary<string, PitTime>> PitTimes { get; set; } = [];

    public sealed partial record PitTime
    {
        public DateTime? Timestamp { get; set; }
        public PitStopEntry PitStop { get; set; } = new();

        public sealed partial record PitStopEntry
        {
            public string? RacingNumber { get; set; }
            public string? PitStopTime { get; set; }
            public string? PitLaneTime { get; set; }
            public string? Lap { get; set; }
        }
    }
}
