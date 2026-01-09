namespace UndercutF1.Data;

public class PositionDataProcessor(IMerger merger) : IProcessor<PositionDataPoint>
{
    public PositionDataPoint Latest { get; private set; } = new();

    public void Process(PositionDataPoint data)
    {
        foreach (var item in data.Position)
        {
            merger.Merge(item.Entries, Latest.Position.Last().Entries);
        }
    }
}
