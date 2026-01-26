namespace UndercutF1.Data;

/// <summary>
/// Sample: {"Meeting": {"Key": 1267,"Name": "Dutch Grand Prix", "OfficialName": "FORMULA 1 HEINEKEN DUTCH GRAND PRIX 2025","Location": "Zandvoort","Number": 15, "Country": {"Key": 133,"Code": "NED","Name": "Netherlands" },"Circuit": {"Key": 55,"ShortName": "Zandvoort" } },"SessionStatus": "Inactive","ArchiveStatus": {"Status": "Generating" },"Key": 9920,"Type": "Race","Name": "Race","StartDate": "2025-08-31T15:00:00","EndDate": "2025-08-31T17:00:00","GmtOffset": "02:00:00","Path": "2025/2025-08-31_Dutch_Grand_Prix/2025-08-31_Race/" }
/// </summary>
[Mergeable]
public sealed partial record SessionInfoDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.SessionInfo;

    public int? Key { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? GmtOffset { get; set; }
    public string? Path { get; set; }

    public MeetingDetail Meeting { get; set; } = new();

    /// <summary>
    /// Populated manually by UndercutF1.Data when the data is first processed, from an external API provider.
    /// Not provided by F1.
    /// </summary>
    [MergeableIgnore]
    public List<(int x, int y)> CircuitPoints { get; set; } = [];

    /// <summary>
    /// Populated manually by UndercutF1.Data when the data is first processed, from an external API provider.
    /// Not provided by F1.
    /// </summary>
    [MergeableIgnore]
    public List<(int number, float x, float y)> CircuitCorners { get; set; } = [];

    /// <summary>
    /// Populated manually by UndercutF1.Data when the data is first processed, from an external API provider.
    /// The rotation that should be applied to the circuit image to make it match the F1 visualisation.
    /// </summary>
    [MergeableIgnore]
    public int CircuitRotation { get; set; } = 0;

    public sealed partial record MeetingDetail
    {
        public string? Name { get; set; }

        public CircuitDetail Circuit { get; set; } = new();

        public sealed partial record CircuitDetail
        {
            public int? Key { get; set; }
            public string? ShortName { get; set; }
        }
    }
}
