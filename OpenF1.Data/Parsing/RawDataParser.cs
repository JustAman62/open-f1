using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class RawDataParser : IRawDataParser
{
    private readonly ILogger<RawDataParser> _logger;

    public RawDataParser(ILogger<RawDataParser> logger) => _logger = logger;

    public LiveTimingDataPoint? ParseRawData(RawTimingDataPoint dataPoint)
    {
        if (!Enum.TryParse<LiveTimingDataType>(dataPoint.EventType.Replace(".z", string.Empty), out var dataType))
        {
            _logger.LogError("Invalid data type provided {}, could not parse", dataPoint.EventType);
            return null;
        }

        LiveTimingDataPoint? parsed = dataType switch
        {
            LiveTimingDataType.Heartbeat => new HeartbeatDataPoint(dataPoint.EventData, dataPoint.LoggedDateTime),
            _ => null
        };

        if (parsed is null)
        {
            _logger.LogError("Could not parse data of type {}.", dataType);
        }
    }
}

