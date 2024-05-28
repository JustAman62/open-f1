namespace OpenF1.Data;

/// <summary>
/// Sample:
/// <c>
/// {
///    "Captures": [
///      {
///        "Utc": "2024-05-26T12:15:25.71Z",
///        "RacingNumber": "81",
///        "Path": "TeamRadio/OSCPIA01_81_20240525_171518.mp3"
///      },
///      {
///        "Utc": "2024-05-26T12:15:25.8662788Z",
///        "RacingNumber": "4",
///        "Path": "TeamRadio/LANNOR01_4_20240526_141522.mp3"
///      }
///    ]
/// }
/// </c>
/// </summary>
public sealed class TeamRadioDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TeamRadio;

    public Dictionary<string, Capture> Captures { get; set; } = new();

    public sealed record Capture
    {
        public DateTimeOffset? Utc { get; set; }
        public string? RacingNumber { get; set; }
        public string? Path { get; set; }
    }
}
