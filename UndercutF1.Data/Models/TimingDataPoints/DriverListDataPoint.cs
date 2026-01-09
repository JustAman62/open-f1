namespace UndercutF1.Data;

/// <summary>
/// Sample:
/// <c>
/// "4": {
///   "RacingNumber": "4",
///   "BroadcastName": "L NORRIS",
///   "FullName": "Lando NORRIS",
///   "Tla": "NOR",
///   "Line": 2,
///   "TeamName": "McLaren",
///   "TeamColour": "F58020",
///   "FirstName": "Lando",
///   "LastName": "Norris",
///   "Reference": "LANNOR01",
///   "HeadshotUrl": "https://www.formula1.com/content/dam/fom-website/drivers/L/LANNOR01_Lando_Norris/lannor01.png.transform/1col/image.png",
///   "CountryCode": "GBR"
/// }
/// </c>
/// </summary>
[Mergeable]
public sealed partial class DriverListDataPoint
    : Dictionary<string, DriverListDataPoint.Driver>,
        ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.DriverList;

    public sealed partial record Driver
    {
        public string? RacingNumber { get; set; }
        public string? BroadcastName { get; set; }
        public string? FullName { get; set; }
        public string? Tla { get; set; }

        /// <summary>
        /// The same as the driver position in <see cref="TimingDataPoint.Driver.Line" />,
        /// however unlike that property this only gets updated at the end of every lap.
        /// </summary>
        public int? Line { get; set; }

        public string? TeamName { get; set; }
        public string? TeamColour { get; set; }

        /// <summary>
        /// Internal property which identifiers whether this driver is "selected" or not.
        /// Unselected drivers are hidden in some displays.
        /// </summary>
        [MergeableIgnore]
        public bool IsSelected { get; set; } = true;
    }
}
