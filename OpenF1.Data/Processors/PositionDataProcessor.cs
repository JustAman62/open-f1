using AutoMapper;

namespace OpenF1.Data;

public class PositionDataProcessor(IMapper mapper) : IProcessor<PositionDataPoint>
{
    public PositionDataPoint Latest { get; private set; } = new();

    public void Process(PositionDataPoint data)
    {
        foreach (var item in data.Position)
        {
            mapper.Map(item.Entries, Latest.Position.Last().Entries);
        }
    }
}
