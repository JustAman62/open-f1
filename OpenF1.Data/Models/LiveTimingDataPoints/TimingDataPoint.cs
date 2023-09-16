using System.Text.Json;

namespace OpenF1.Data;

public sealed record TimingDataPoint : LiveTimingDataPoint
{
    public override LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingData;

    public TimingDataPoint(TimingData data, string sessionName, DateTimeOffset loggedDateTime)
        : base(sessionName, loggedDateTime)
        => Data = data;

    public TimingData Data { get; set; }

    public sealed record TimingData
    {
        public Dictionary<string, Driver> Lines { get; set; } = new();

        public sealed record Driver
        {
            /// <summary>
            /// For the leader, this is the lap number e.g. <c>LAP 54</c>,
            /// but everyone else is a time in the format <c>+1.123</c>,
            /// or if more than a lap down then <c>5L</c> (i.e. 5 laps behind).
            /// </summary>
            public string? GapToLeader { get; set; }
            public Interval? IntervalToPositionAhead { get; set; }

            public int? Line { get; set; }
            public string? Position { get; set; }

            public bool? InPit { get; set; }
            public bool? PitOut { get; set; }
            public int? NumberOfPitStops { get; set; }

            public int? NumberOfLaps { get; set; }
            public LapSectorTime? LastLapTime { get; set; }

            public Dictionary<string, LapSectorTime> Sectors { get; set; } = new();

            public sealed record Interval
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
            public sealed record LapSectorTime
            {
                public string? Value { get; set; }
                public bool? OverallFastest { get; set; }
                public bool? PersonalFastest { get; set; }
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }
    }
}
