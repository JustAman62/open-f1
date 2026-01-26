using System.Text.Json.Nodes;

namespace UndercutF1.Data;

public record struct RawTimingDataPoint(string Type, JsonNode Json, DateTimeOffset DateTime);
