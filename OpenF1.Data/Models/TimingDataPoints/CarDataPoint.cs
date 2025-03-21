using System.Text.Json.Serialization;

namespace OpenF1.Data;

/// <summary>
/// Car data is sent as compressed (with deflate) JSON containing Entries.
/// Each Entry is all the car data for a specific point in time, and they seem to be batched to reduce network load.
/// </summary>
public sealed class CarDataPoint : ILiveTimingDataPoint
{
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.CarData;

    public List<Entry> Entries { get; set; } = new();

    public sealed class Entry
    {
        public DateTimeOffset Utc { get; set; }

        public Dictionary<string, Car> Cars { get; set; } = new();

        public sealed class Car
        {
            public Channel Channels { get; set; } = new();

            public sealed class Channel
            {
                [JsonPropertyName("0")]
                public int? Rpm { get; set; }

                [JsonPropertyName("2")]
                public int? Speed { get; set; }

                [JsonPropertyName("3")]
                public int? Ngear { get; set; }

                [JsonPropertyName("4")]
                public int? Throttle { get; set; }

                [JsonPropertyName("5")]
                public int? Brake { get; set; }

                /// <summary>
                /// From FastF1s understanding of this field:
                /// - DRS: 0-14 (Odd DRS is Disabled, Even DRS is Enabled?)
                ///  - 0 =  Off
                ///  - 1 =  Off
                ///  - 2 =  (?)
                ///  - 3 =  (?)
                ///  - 8 =  Detected, Eligible once in Activation Zone (Noted Sometimes)
                ///  - 10 = On (Unknown Distinction)
                ///  - 12 = On (Unknown Distinction)
                ///  - 14 = On (Unknown Distinction)
                /// </summary>
                [JsonPropertyName("45")]
                public int? Drs { get; set; }
            }
        }
    }
}
