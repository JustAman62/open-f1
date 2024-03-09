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
            var cloned = mapper.Map<TimingDataPoint.Driver>(lap);
            var lapDrivers = DriversByLap.GetValueOrDefault(lap.NumberOfLaps!.Value);
            if (lapDrivers is null)
            {
                lapDrivers = [];
                DriversByLap.TryAdd(lap.NumberOfLaps!.Value, lapDrivers);
            }

            DriversByLap[lap.NumberOfLaps!.Value].TryAdd(driverNumber, cloned);
        }
    }
}
