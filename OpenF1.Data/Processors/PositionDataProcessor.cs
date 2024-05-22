namespace OpenF1.Data;

public class PositionDataProcessor : IProcessor<PositionDataPoint>
{
    public PositionDataPoint Latest { get; private set; } = new();

    public void Process(PositionDataPoint data)
    {
        Latest.Position.AddRange(data.Position);
        // Remove all but the latest entry
        if (Latest.Position.Count > 1)
        {
            Latest.Position.RemoveRange(0, Latest.Position.Count - 1);
        }
    }
}
