namespace UndercutF1.Data;

/// <summary>
/// Maintains the latest state of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="ILiveTimingDataPoint"/> to process.</typeparam>
/// <param name="merger">The <see cref="IMerger"/> used to map the data points on to each other.</param>
public class ProcessorBase<T>(IMerger merger) : IProcessor<T>
    where T : ILiveTimingDataPoint, new()
{
    public T Latest { get; private set; } = new();

    public virtual void Process(T data) => merger.Merge(data, Latest);
}
