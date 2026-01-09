namespace UndercutF1.Data;

[Mergeable]
public sealed partial record TimingStatsDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingStats;

    public Dictionary<string, Driver> Lines { get; set; } = new();

    public sealed partial record Driver
    {
        public Dictionary<string, Stat> BestSpeeds { get; set; } = [];

        public partial record Stat
        {
            public string? Value { get; set; }
            public int? Position { get; set; }
        }
    }
}
