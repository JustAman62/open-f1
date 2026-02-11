using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace UndercutF1.Data;

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(StringToBoolConverter)]
)]
[JsonSerializable(typeof(RawTimingDataPoint))]
[JsonSerializable(typeof(CarDataPoint))]
[JsonSerializable(typeof(CarDataPoint.Entry), TypeInfoPropertyName = "CarDataPoint_Entry")]
[JsonSerializable(typeof(ChampionshipPredictionDataPoint))]
[JsonSerializable(
    typeof(Dictionary<string, ChampionshipPredictionDataPoint.Driver>),
    TypeInfoPropertyName = "ChampionshipPredictionDataPointDriverDictionary"
)]
[JsonSerializable(
    typeof(ChampionshipPredictionDataPoint.Driver),
    TypeInfoPropertyName = "ChampionshipPredictionDataPoint_Driver"
)]
[JsonSerializable(typeof(DriverListDataPoint))]
[JsonSerializable(
    typeof(DriverListDataPoint.Driver),
    TypeInfoPropertyName = "DriverListDataPoint_Driver"
)]
[JsonSerializable(typeof(ExtrapolatedClockDataPoint))]
[JsonSerializable(typeof(HeartbeatDataPoint))]
[JsonSerializable(typeof(LapCountDataPoint))]
[JsonSerializable(typeof(PitLaneTimeCollectionDataPoint))]
[JsonSerializable(
    typeof(PitLaneTimeCollectionDataPoint.PitTime),
    TypeInfoPropertyName = "PitLaneTimeCollectionDataPoint_PitTime"
)]
[JsonSerializable(typeof(PitStopSeriesDataPoint))]
[JsonSerializable(
    typeof(Dictionary<string, PitStopSeriesDataPoint.PitTime>),
    TypeInfoPropertyName = "PitStopSeriesDataPoint_PitTimeDictionary"
)]
[JsonSerializable(typeof(PositionDataPoint))]
[JsonSerializable(
    typeof(PositionDataPoint.PositionData.Entry),
    TypeInfoPropertyName = "PositionDataPoint_PositionData_Entry"
)]
[JsonSerializable(typeof(RaceControlMessageDataPoint))]
[JsonSerializable(typeof(SessionInfoDataPoint))]
[JsonSerializable(typeof(TeamRadioDataPoint))]
[JsonSerializable(typeof(TimingAppDataPoint))]
[JsonSerializable(
    typeof(Dictionary<string, TimingAppDataPoint.Driver>),
    TypeInfoPropertyName = "TimingAppDataPointDriverDictionary"
)]
[JsonSerializable(typeof(TimingDataPoint))]
[JsonSerializable(typeof(TimingDataPoint.Driver), TypeInfoPropertyName = "TimingDataPoint_Driver")]
[JsonSerializable(
    typeof(Dictionary<string, TimingDataPoint.Driver>),
    TypeInfoPropertyName = "TimingDataPointDriverDictionary"
)]
[JsonSerializable(typeof(TimingStatsDataPoint))]
[JsonSerializable(
    typeof(TimingStatsDataPoint.Driver),
    TypeInfoPropertyName = "TimingStatsDataPoint_Driver"
)]
[JsonSerializable(
    typeof(Dictionary<string, TimingStatsDataPoint.Driver>),
    TypeInfoPropertyName = "TimingStatsDataPointDriverDictionary"
)]
[JsonSerializable(typeof(TrackStatusDataPoint))]
[JsonSerializable(typeof(WeatherDataPoint))]
[JsonSerializable(typeof(Formula1Account.TokenPayload))] // For SignalR messages
[JsonSerializable(typeof(string[]))] // For SignalR messages
[JsonSerializable(typeof(JsonObject))] // For SignalR messages
[JsonSerializable(typeof(ListMeetingsApiResponse))] // For IDataImporter
[JsonSerializable(typeof(Dictionary<string, JsonNode>))]
public partial class TimingDataSerializerContext : JsonSerializerContext
{
    public static TimingDataSerializerContext Pretty => LazyPretty.Value;

    public static Lazy<TimingDataSerializerContext> LazyPretty { get; } =
        new(() =>
            new(
                new(JsonSerializerDefaults.Web)
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                    AllowTrailingCommas = true,
                    WriteIndented = false,
                    Converters = { new StringToBoolConverter() },
                }
            )
        );

    public static TimingDataSerializerContext Raw => LazyRaw.Value;

    public static Lazy<TimingDataSerializerContext> LazyRaw { get; } =
        new(() =>
            new(
                new(JsonSerializerDefaults.Web)
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                    AllowTrailingCommas = true,
                    WriteIndented = false,
                    Converters = { new StringToBoolConverter() },
                }
            )
        );
}
