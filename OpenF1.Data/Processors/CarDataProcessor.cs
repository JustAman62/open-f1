namespace OpenF1.Data;

public class CarDataProcessor : IProcessor<CarDataPoint>
{
    public CarDataPoint Latest { get; private set; } = new();

    public void Process(CarDataPoint data)
    {
        Latest.Entries.AddRange(data.Entries);
        // Remove all but the latest entry
        if (Latest.Entries.Count > 1)
        {
            Latest.Entries.RemoveRange(0, Latest.Entries.Count - 1);
        }
    }
}
