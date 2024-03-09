using AutoMapper;

namespace OpenF1.Data;

public class TimingDataProcessor(IMapper mapper) : IProcessor<TimingDataPoint>
{
    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    public void Process(TimingDataPoint data)
    {
        if (LatestLiveTimingDataPoint is null)
        {
            LatestLiveTimingDataPoint = mapper.Map<TimingDataPoint>(data);
        }
        else
        {
            mapper.Map(data, LatestLiveTimingDataPoint);
        }
    }
}
