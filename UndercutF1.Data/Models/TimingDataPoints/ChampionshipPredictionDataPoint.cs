namespace UndercutF1.Data;

/// <summary>
/// Sample:
/// <c>
///    "Drivers": {
///      "1": {
///        "RacingNumber": "1",
///        "CurrentPosition": 1,
///        "PredictedPosition": 1,
///        "CurrentPoints": 161.0,
///        "PredictedPoints": 169.0
///      },
///      "16": {
///        "RacingNumber": "16",
///        "CurrentPosition": 2,
///        "PredictedPosition": 2,
///        "CurrentPoints": 113.0,
///        "PredictedPoints": 138.0
///      }
/// </c>
/// </summary>
[Mergeable]
public sealed partial record ChampionshipPredictionDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.ChampionshipPrediction;

    public Dictionary<string, Driver> Drivers { get; set; } = new();
    public Dictionary<string, Team> Teams { get; set; } = new();

    public sealed partial record Driver
    {
        public string? RacingNumber { get; set; }
        public int? CurrentPosition { get; set; }
        public int? PredictedPosition { get; set; }
        public decimal? CurrentPoints { get; set; }
        public decimal? PredictedPoints { get; set; }
    }

    public sealed partial record Team
    {
        public string? TeamName { get; set; }
        public int? CurrentPosition { get; set; }
        public int? PredictedPosition { get; set; }
        public decimal? CurrentPoints { get; set; }
        public decimal? PredictedPoints { get; set; }
    }
}
