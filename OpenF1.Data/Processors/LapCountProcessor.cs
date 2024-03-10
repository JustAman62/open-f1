using AutoMapper;

namespace OpenF1.Data;

public class LapCountProcessor(IMapper mapper) : IProcessor<LapCountDataPoint>
{
    public LapCountDataPoint? Latest { get; private set; }

    public void Process(LapCountDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<LapCountDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
