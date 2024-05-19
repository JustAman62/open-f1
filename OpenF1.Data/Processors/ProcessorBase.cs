using AutoMapper;

namespace OpenF1.Data;

/// <summary>
/// Maintains the latest state of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="ILiveTimingDataPoint"/> to process.</typeparam>
/// <param name="mapper">The <see cref="IMapper"/> used to map the data points on to each other.</param>
public class ProcessorBase<T>(IMapper mapper) : IProcessor<T>
    where T : new()
{
    public T Latest { get; private set; } = new();

    public void Process(T data) => mapper.Map(data, Latest);
}
