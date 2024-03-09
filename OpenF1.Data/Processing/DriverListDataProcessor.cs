using AutoMapper;

namespace OpenF1.Data;

public class DriverListDataProcessor(IMapper mapper) : IProcessor<DriverListDataPoint>
{
    public DriverListDataPoint? Latest { get; private set; }

    public void Process(DriverListDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<DriverListDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
