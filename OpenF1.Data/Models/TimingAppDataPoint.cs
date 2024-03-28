using System.Text.Json;

namespace OpenF1.Data;

/// <summary>
/// Sample:
/// "Lines": {
///   "1": {
///     "RacingNumber": "1",
///     "Line": 1,
///     "Stints": [
///       {
///         "LapFlags": 0,
///         "Compound": "SOFT",
///         "New": "true",
///         "TyresNotChanged": "0",
///         "TotalLaps": 3,
///         "StartLaps": 0,
///         "LapTime": "1:28.491",
///         "LapNumber": 3
///       }
///   }
/// }
/// </summary>
public sealed record TimingAppDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingAppData;

    public Dictionary<string, Driver> Lines { get; set; } = new();

    public sealed record Driver
    {
        public Dictionary<string, Stint> Stints { get; set; } = new();

        public sealed record Stint
        {
            public int? LapFlags { get; set; }
            public string? Compound { get; set; }
            public string? New { get; set; }
            public string? TyresNotChanged { get; set; }
            public int? TotalLaps { get; set; }
            public int? StartLaps { get; set; }
            public string? LapTime { get; set; }
            public int? LapNumber { get; set; }
        }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
