using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class TimingDataProcessor : IProcessor<TimingDataPoint>
{
    private readonly IMapper _mapper;
    private readonly ILogger<TimingDataProcessor> _logger;

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    public TimingDataProcessor(
        IMapper mapper,
        ILogger<TimingDataProcessor> logger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    private Task ProcessAsync(TimingDataPoint data)
    {
        if (LatestLiveTimingDataPoint is null)
        {
            LatestLiveTimingDataPoint = _mapper.Map<TimingDataPoint>(data);
        }
        else
        {
            _mapper.Map(data, LatestLiveTimingDataPoint);
        }

        return Task.CompletedTask;
    }
}

