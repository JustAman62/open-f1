namespace UndercutF1.Data;

/// <summary>
/// Position data is sent as compressed (with deflate) JSON containing Entries.
/// Each Position Entry is the cars position at a specific point of time, and they seem to be batched to reduce network load.
/// </summary>
[Mergeable]
public sealed partial record PositionDataPoint : ILiveTimingDataPoint
{
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.Position;

    public List<PositionData> Position { get; set; } = [new()];

    public sealed partial record PositionData
    {
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Dictionary of DriverNumber to Entry with position data.
        /// </summary>
        public Dictionary<string, Entry> Entries { get; set; } = [];

        public sealed partial record Entry
        {
            public DriverStatus? Status { get; set; }

            /// <summary>
            /// X position of the car in 1/10ths of a meter (cm).
            /// </summary>
            public int? X { get; set; }

            /// <summary>
            /// Y position of the car in 1/10ths of a meter (cm).
            /// </summary>
            public int? Y { get; set; }

            /// <summary>
            /// Z position of the car in 1/10ths of a meter (cm).
            /// </summary>
            public int? Z { get; set; }

            public enum DriverStatus
            {
                OnTrack,
                OffTrack,
            }
        }
    }
}
