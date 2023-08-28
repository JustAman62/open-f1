using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class RawDataParser : IRawDataParser
{
    private readonly ILogger<RawDataParser> _logger;

    public RawDataParser(ILogger<RawDataParser> logger) => _logger = logger;

    public LiveTimingDataPoint? ParseRawTimingDataPoint(RawTimingDataPoint dataPoint)
    {
        try
        {
            if (!Enum.TryParse<LiveTimingDataType>(dataPoint.EventType.Replace(".z", string.Empty), out var dataType))
            {
                _logger.LogError("Invalid data type provided {}, could not parse", dataPoint.EventType);
                return null;
            }

            if (dataPoint.EventType.EndsWith(".z"))
            {
                // TODO: Unzip the data before proceeding
            }

            LiveTimingDataPoint? parsed = dataType switch
            {
                LiveTimingDataType.Heartbeat => new HeartbeatDataPoint(JsonSerializer.Deserialize<HeartbeatDataPoint.HeartbeatData>(dataPoint.EventData)!, dataPoint.LoggedDateTime),
                LiveTimingDataType.TimingData => new TimingDataPoint(JsonSerializer.Deserialize<TimingDataPoint.TimingData>(dataPoint.EventData)!, dataPoint.LoggedDateTime),
                _ => null
            };

            if (parsed is null)
            {
                _logger.LogError("Could not parse raw data of type {}.", dataType);
            }

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown when parsing {}", nameof(RawTimingDataPoint));
            return null;
        }
    }
}

