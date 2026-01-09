namespace UndercutF1.Data;

/// <summary>
/// Maintains the latest state of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="ILiveTimingDataPoint"/> to process.</typeparam>
public abstract class ProcessorBase<T>() : IProcessor<T>
    where T : ILiveTimingDataPoint, IMergeable<T>, new()
{
    public T Latest { get; private set; } = new();

    public virtual void Process(T data) => Latest.MergeWith(data);
}
