using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class TimingDataProcessor(IMapper mapper, ILogger<TimingDataProcessor> logger)
    : IProcessor<TimingDataPoint>
{
    /// <summary>
    /// The latest timing data available
    /// </summary>
    public TimingDataPoint LatestLiveTimingDataPoint { get; private set; } = new();

    /// <summary>
    /// Dictionary of LapNumber-DriverList where DriverList is Dictionary DriverNumber-Lap.
    /// </summary>
    public Dictionary<int, Dictionary<string, TimingDataPoint.Driver>> DriversByLap
    {
        get;
        private set;
    } = new();

    /// <summary>
    /// Dictionary of DriverNumber-Lap where each entry is the best lap so far for that DriverNumber.
    /// </summary>
    public Dictionary<string, TimingDataPoint.Driver> BestLaps { get; private set; } = new();

    public void Process(TimingDataPoint data)
    {
        _ = mapper.Map(data, LatestLiveTimingDataPoint);

        // If this update changes the NumberOfLaps, then take a snapshot of that drivers data for that lap
        foreach (var (driverNumber, lap) in data.Lines.Where(x => x.Value.NumberOfLaps.HasValue))
        {
            var lapDrivers = DriversByLap.GetValueOrDefault(lap.NumberOfLaps!.Value);
            if (lapDrivers is null)
            {
                lapDrivers = [];
                DriversByLap.TryAdd(lap.NumberOfLaps!.Value, lapDrivers);
            }

            // Super hacky way of doing a clean clone. Using AutoMapper seems to not clone the Sectors array properly.
            var cloned = JsonSerializer.Deserialize<TimingDataPoint.Driver>(
                JsonSerializer.Serialize(LatestLiveTimingDataPoint.Lines[driverNumber])
            )!;
            DriversByLap[lap.NumberOfLaps!.Value].TryAdd(driverNumber, cloned);

            if (!string.IsNullOrWhiteSpace(cloned.BestLapTime?.Value))
            {
                // Check for an existing best lap for this driver. If its faster, update it.
                if (BestLaps.TryGetValue(driverNumber, out var existingBestLap))
                {
                    logger.LogWarning($"ExistingLap: {existingBestLap}");
                    var newLapTimeSpan = cloned.BestLapTime?.ToTimeSpan();
                    var existingBestLapTimeSpan = existingBestLap.BestLapTime.ToTimeSpan();
                    if (
                        newLapTimeSpan.HasValue
                        && existingBestLapTimeSpan.HasValue
                        && newLapTimeSpan.Value < existingBestLapTimeSpan.Value
                    )
                    {
                        BestLaps[driverNumber] = cloned;
                    }
                }
                else
                {
                    BestLaps.TryAdd(driverNumber, cloned);
                }
            }
            else
            {
                // If the BestLapTime is wiped, remove the entry
                // This usually happens between qualifying sessions
                _ = BestLaps.Remove(driverNumber);
            }
        }
    }
}
