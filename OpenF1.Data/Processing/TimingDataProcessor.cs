using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class TimingDataProcessor(IMapper mapper, ILogger<TimingDataProcessor> logger)
    : IProcessor<TimingDataPoint>
{
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<TimingDataProcessor> _logger = logger;

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    public void Process(TimingDataPoint data)
    {
        if (LatestLiveTimingDataPoint is null)
        {
            LatestLiveTimingDataPoint = _mapper.Map<TimingDataPoint>(data);
        }
        else
        {
            _mapper.Map(data, LatestLiveTimingDataPoint);
        }
    }
}
