using AutoMapper;

namespace OpenF1.Data;

public class TrackStatusProcessor(IMapper mapper) : IProcessor<TrackStatusDataPoint>
{
    public TrackStatusDataPoint? Latest { get; private set; }

    public void Process(TrackStatusDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<TrackStatusDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
