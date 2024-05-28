using AutoMapper;

namespace OpenF1.Data;

public class TeamRadioProcessor(IMapper mapper) : ProcessorBase<TeamRadioDataPoint>(mapper)
{
    public Dictionary<string, TeamRadioDataPoint.Capture> Ordered =>
        Latest.Captures.Reverse().Take(8).ToDictionary(x => x.Key, x => x.Value);
}
