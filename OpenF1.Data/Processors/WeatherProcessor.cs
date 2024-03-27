using AutoMapper;

namespace OpenF1.Data;

public class WeatherProcessor(IMapper mapper) : IProcessor<WeatherDataPoint>
{
    public WeatherDataPoint? Latest { get; private set; }

    public void Process(WeatherDataPoint data)
    {
        if (Latest is null)
        {
            Latest = mapper.Map<WeatherDataPoint>(data);
        }
        else
        {
            mapper.Map(data, Latest);
        }
    }
}
