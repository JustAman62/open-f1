using System.Text.Json;

namespace UndercutF1.Data;

public class TimingDataProcessor() : IProcessor<TimingDataPoint>
{
    /// <summary>
    /// The latest timing data available
    /// </summary>
    public TimingDataPoint Latest { get; private set; } = new();

    /// <summary>
    /// Dictionary of LapNumber-DriverList where DriverList is Dictionary DriverNumber-Lap.
    /// </summary>
    public Dictionary<int, Dictionary<string, TimingDataPoint.Driver>> DriversByLap
    {
        get;
        private set;
    } = [];

    /// <summary>
    /// Dictionary of DriverNumber-Lap where each entry is the best lap so far for that DriverNumber.
    /// </summary>
    public Dictionary<string, TimingDataPoint.Driver> BestLaps { get; private set; } = new();

    public void Process(TimingDataPoint data)
    {
        Latest.MergeWith(data);

        foreach (var (driverNumber, lapUpdate) in data.Lines)
        {
            PushCustomProperties(driverNumber, lapUpdate);

            // Super hacky way of doing a clean clone. Using AutoMapper seems to not clone the Sectors array properly.
            // We need this clone because we want to store a snapshot of the lap which means we don't want to store the
            // same reference.
            var cloned = JsonSerializer.Deserialize(
                JsonSerializer.Serialize(
                    Latest.Lines[driverNumber],
                    TimingDataSerializerContext.Raw.TimingDataPoint_Driver
                ),
                TimingDataSerializerContext.Raw.TimingDataPoint_Driver
            )!;

            // If this update changes the NumberOfLaps, then take a snapshot of that drivers data for that lap
            if (lapUpdate.NumberOfLaps.HasValue)
            {
                HandleNewLap(driverNumber, lapUpdate, cloned);
            }

            // This update contains a new best lap
            if (!string.IsNullOrWhiteSpace(lapUpdate.BestLapTime?.Value))
            {
                HandleNewBestLap(driverNumber, lapUpdate, cloned);
            }

            if (string.IsNullOrWhiteSpace(cloned.BestLapTime?.Value))
            {
                // If the BestLapTime is wiped, remove the entry
                // This usually happens between qualifying sessions
                _ = BestLaps.Remove(driverNumber);
            }
        }
    }

    private void HandleNewBestLap(
        string driverNumber,
        TimingDataPoint.Driver partialUpdate,
        TimingDataPoint.Driver updated
    )
    {
        // Check for an existing best lap for this driver. If its faster, update it.
        if (BestLaps.TryGetValue(driverNumber, out var existingBestLap))
        {
            var newLapTimeSpan = partialUpdate.BestLapTime?.ToTimeSpan();
            var existingBestLapTimeSpan = existingBestLap.BestLapTime.ToTimeSpan();
            if (
                newLapTimeSpan.HasValue
                && existingBestLapTimeSpan.HasValue
                && newLapTimeSpan.Value < existingBestLapTimeSpan.Value
            )
            {
                BestLaps[driverNumber] = updated;
            }
        }
        else
        {
            BestLaps.TryAdd(driverNumber, updated);
        }
    }

    private void HandleNewLap(
        string driverNumber,
        TimingDataPoint.Driver partialUpdate,
        TimingDataPoint.Driver updated
    )
    {
        var lapDrivers = DriversByLap.GetValueOrDefault(partialUpdate.NumberOfLaps!.Value);
        if (lapDrivers is null)
        {
            lapDrivers = [];
            DriversByLap.TryAdd(partialUpdate.NumberOfLaps!.Value, lapDrivers);
        }

        DriversByLap[partialUpdate.NumberOfLaps!.Value].TryAdd(driverNumber, updated);

        if (updated.PitOut != true && updated.InPit != true)
        {
            // No longer a pit in/out lap, so unset the property
            Latest.Lines.GetValueOrDefault(driverNumber)!.IsPitLap = false;
        }
    }

    private void PushCustomProperties(string driverNumber, TimingDataPoint.Driver partialUpdate)
    {
        var line = Latest.Lines.GetValueOrDefault(driverNumber);
        if (line is null)
            return;

        // If the update indicates this is a pit lap, then mark the current data point as a pit lap
        if (partialUpdate.PitOut.GetValueOrDefault() || partialUpdate.InPit.GetValueOrDefault())
        {
            line.IsPitLap = true;
        }

        // Push down the SessionPart property if it exists and is not already set
        if (line.SessionPart != Latest.SessionPart)
        {
            line.SessionPart = Latest.SessionPart;
        }
    }
}
