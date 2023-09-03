using System.Text.Json.Nodes;

namespace OpenF1.Data;

public record RawTimingDataPoint
{
    public long Id { get; set; }
    public string SessionName { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime LoggedDateTime { get; set; }

    public static RawTimingDataPoint? Parse(string rawData, string sessionName)
    {
        try
        {
            var json = JsonNode.Parse(rawData);
            var data = json?["A"];

            if (data is null) return null;

            if (data.AsArray().Count < 3)
            {
                return null;
            }

            var eventData = data[1] is JsonValue
                ? data[1]!.ToString()
                : data[1]!.ToJsonString();

            return new()
            {
                EventType = data[0]!.ToString(),
                EventData = eventData,
                LoggedDateTime = DateTime.Parse(data[2]!.ToString()),
            };

        }
        catch (Exception)
        {
            return null;
        }
    }
}

