namespace OpenF1.Data;

public interface IProcessor
{
}

public interface IProcessor<T> : IProcessor
    where T : ILiveTimingDataPoint
{
    Type InputType => typeof(T);

    T Latest { get; }

    void Process(T data);
}
