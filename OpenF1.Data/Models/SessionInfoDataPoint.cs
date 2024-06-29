namespace OpenF1.Data;

/// <summary>
/// Sample: {"Messages": {"2": {"Utc": "2021-03-27T12:00:00", "Category": "Flag", "Flag": "GREEN", "Scope": "Track", "Message": "GREEN LIGHT - PIT EXIT OPEN"}}}
/// </summary>
public sealed record SessionInfoDataPoint: ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.SessionInfo;

    public int? Key { get; init; }
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public string? GmtOffset { get; init; }
    public string? Path { get; init; }

    public MeetingDetail? Meeting { get; init; }

    /// <summary>
    /// Populated manually by OpenF1.Data when the data is first processed, from an external API provider.
    /// Not provided by F1.
    /// </summary>
    public List<(int x, int y)> CircuitPoints { get; set; } = [];

    public sealed record MeetingDetail
    {
        public string? Name { get; init; }

        public CircuitDetail? Circuit { get; init; }

        public sealed record CircuitDetail
        {
            public int? Key { get; init; }
            public string? ShortName { get; init; }
        }
    }
}
