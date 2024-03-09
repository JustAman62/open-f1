using AutoMapper;

namespace OpenF1.Data;

public class TimingAppDataProcessor(IMapper mapper) : IProcessor<TimingAppDataPoint>
{
    public TimingAppDataPoint? Latest { get; private set; }

    public void Process(TimingAppDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<TimingAppDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
