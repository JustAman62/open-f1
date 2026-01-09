namespace UndercutF1.Data;

[Mergeable]
public sealed partial record TimingDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingData;

    /// <summary>
    /// In qualifying sessions, this is set to 1 for Q1, 2 for Q2, etc.
    /// <c>null</c> in all other sessions.
    /// </summary>
    public int? SessionPart { get; set; }

    public Dictionary<string, Driver> Lines { get; set; } = new();

    public sealed partial record Driver
    {
        /// <summary>
        /// For the leader, this is the lap number e.g. <c>LAP 54</c>,
        /// but everyone else is a time in the format <c>+1.123</c>,
        /// or if more than a lap down then <c>5L</c> (i.e. 5 laps behind).
        /// </summary>
        public string? GapToLeader { get; set; }
        public Interval IntervalToPositionAhead { get; set; } = new();

        public int? Line { get; set; }
        public string? Position { get; set; }

        public bool? InPit { get; set; }
        public bool? PitOut { get; set; }
        public int? NumberOfPitStops { get; set; }

        /// <summary>
        /// A custom property where we track if the current lap had <see cref="InPit"/> or <see cref="PitOut"/>
        /// set at any time.
        ///
        /// The intention of the property is to allow for easy filtering of non-flying laps from lap-by-lap data.
        /// </summary>
        [MergeableIgnore]
        public bool IsPitLap { get; set; }

        /// <summary>
        /// A custom property which indicates which part of Qualifying this lap was set in.
        /// Only set after a lap is completed.
        /// <c>null</c> in all non-qualifying sessions.
        /// Value is pushed down from <see cref="TimingDataPoint.SessionPart"/>.
        /// </summary>
        [MergeableIgnore]
        public int? SessionPart { get; set; }

        public int? NumberOfLaps { get; set; }
        public LapSectorTime LastLapTime { get; set; } = new();

        public Dictionary<string, LapSectorTime> Sectors { get; set; } = new();

        public BestLap BestLapTime { get; set; } = new();

        /// <summary>
        /// In qualifying, indicates if the driver is knocked out of qualifying
        /// </summary>
        public bool? KnockedOut { get; set; }

        /// <summary>
        /// In race sessions, indicates if the driver has retired
        /// </summary>
        public bool? Retired { get; set; }

        /// <summary>
        /// Whether the car has stopped or not. Usually means retried.
        /// </summary>
        public bool? Stopped { get; set; }

        /// <summary>
        /// This is actually a flags enum
        /// </summary>
        public StatusFlags? Status { get; set; }

        public sealed partial record Interval
        {
            /// <summary>
            /// Can be in the format <c>+1.123</c>,
            /// or if more than a lap then <c>5L</c> (i.e. 5 laps behind)
            /// </summary>
            public string? Value { get; set; }
            public bool? Catching { get; set; }
        }

        /// <summary>
        /// Represents both Laps and Sectors (same model in different places)
        /// </summary>
        public sealed partial record LapSectorTime
        {
            public string? Value { get; set; }
            public bool? OverallFastest { get; set; }
            public bool? PersonalFastest { get; set; }
            public Dictionary<int, Segment> Segments { get; set; } = new();

            public sealed partial record Segment
            {
                public StatusFlags? Status { get; set; }
            }
        }

        public sealed partial record BestLap
        {
            public string? Value { get; set; }
            public int? Lap { get; set; }
        }

        [Flags]
        public enum StatusFlags
        {
            PersonalBest = 1,
            OverallBest = 2,

            /// <summary>
            /// Went through this mini sector in the pit lane
            /// </summary>
            PitLane = 16,

            /// <summary>
            /// Set when the driver passes the chequered flag in quali or race sessions
            /// </summary>
            ChequeredFlag = 1024,

            /// <summary>
            /// Segment completed. If this is the only flag set, means a yellow segment.
            /// </summary>
            SegmentComplete = 2048,
        }
    }
}
