using System.Text.Json;
using AutoMapper;

namespace OpenF1.Data;

public class TimingDataProcessor(IMapper mapper) : IProcessor<TimingDataPoint>
{
    public TimingDataPoint LatestLiveTimingDataPoint { get; private set; } = new();

    public Dictionary<int, Dictionary<string, TimingDataPoint.Driver>> DriversByLap { get; private set; } = new();

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
            var cloned = JsonSerializer.Deserialize<TimingDataPoint.Driver>(JsonSerializer.Serialize(LatestLiveTimingDataPoint.Lines[driverNumber]))!;
            DriversByLap[lap.NumberOfLaps!.Value].TryAdd(driverNumber, cloned);
        }
    }
}
