namespace UndercutF1.Data;

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
[Mergeable]
public sealed partial record TimingAppDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingAppData;

    public Dictionary<string, Driver> Lines { get; set; } = new();

    public sealed partial record Driver
    {
        /// <summary>
        /// The position the driver started the race from. Only available in Race sessions.
        /// </summary>
        public string? GridPos { get; set; }

        public int? Line { get; set; }

        public Dictionary<string, Stint> Stints { get; set; } = new();

        public sealed partial record Stint
        {
            public int? LapFlags { get; set; }
            public string? Compound { get; set; }
            public bool? New { get; set; }
            public int? TotalLaps { get; set; }
            public int? StartLaps { get; set; }
            public string? LapTime { get; set; }
        }
    }
}
