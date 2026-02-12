using System.Text.Json.Serialization;

namespace UndercutF1.Data;

/// <summary>
/// Car data is sent as compressed (with deflate) JSON containing Entries.
/// Each Entry is all the car data for a specific point in time, and they seem to be batched to reduce network load.
/// </summary>
[Mergeable]
public sealed partial record CarDataPoint : ILiveTimingDataPoint
{
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.CarData;

    public List<Entry> Entries { get; set; } = new();

    public sealed partial record Entry
    {
        public DateTimeOffset Utc { get; set; }

        public Dictionary<string, Car> Cars { get; set; } = new();

        public sealed partial record Car
        {
            public Channel Channels { get; set; } = new();

            public sealed partial record Channel
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
            }
        }
    }
}
