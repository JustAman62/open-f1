using AutoMapper;

namespace OpenF1.Data;

public class SessionInfoProcessor(IMapper mapper) : IProcessor<SessionInfoDataPoint>
{
    public SessionInfoDataPoint Latest { get; private set; } = new();

    public void Process(SessionInfoDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<SessionInfoDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
