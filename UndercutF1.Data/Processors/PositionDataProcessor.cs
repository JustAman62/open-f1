namespace UndercutF1.Data;

public class PositionDataProcessor() : IProcessor<PositionDataPoint>
{
    public PositionDataPoint Latest { get; private set; } = new();

    public void Process(PositionDataPoint data)
    {
        foreach (var item in data.Position)
        {
            Latest.Position.Last().Entries.MergeWith(item.Entries);
        }
    }
}
