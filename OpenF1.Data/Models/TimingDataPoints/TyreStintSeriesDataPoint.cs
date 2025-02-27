namespace OpenF1.Data;

public sealed record TyreStintSeriesDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TyreStintSeries;

    public Dictionary<string, Dictionary<string, Stint>> Stints { get; set; } = [];

    public sealed record Stint
    {
        public int? TotalLaps { get; set; }
        public int? StartLaps { get; set; }
        public string? Compound { get; set; }
        public bool? New { get; set; }
        public int? Position { get; set; }
    }
}
