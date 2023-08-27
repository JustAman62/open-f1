using System.Text.Json.Nodes;

namespace OpenF1.Data;

public record RawTimingDataPoint
{
    public long Id { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime LoggedDateTime { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Multiple validation difficult to read")]
    public static RawTimingDataPoint? Parse(string rawData)
    {
        try
        {
            var json = JsonObject.Parse(rawData);
            var data = json?["A"];

            if (data is null) return null;

            if (data.AsArray().Count < 3)
            {
                return null;
            }

            return new()
            {
                EventType = data[0]!.ToString(),
                EventData = data[1]!.ToString(),
                LoggedDateTime = DateTime.Parse(data[2]!.ToString()),
            };

        }
        catch (Exception)
        {
            return null;
        }
    }
}

