namespace OpenF1.Data;

public sealed record TimingStatsDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingStats;

    public Dictionary<string, Driver> Lines { get; set; } = new();

    public sealed record Driver
    {
        public int? Line { get; set; }
        public string? RacingNumber { get; set; }

        public Dictionary<string, Stat> BestSpeeds { get; set; } = [];

        public record Stat
        {
            public string? Value { get; set; }
            public int? Position { get; set; }
        }
    }
}
