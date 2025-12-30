namespace UndercutF1.Data;

public interface IProcessor
{
    ILiveTimingDataPoint GetLatestData();
}

public interface IProcessor<T> : IProcessor
    where T : ILiveTimingDataPoint
{
    Type InputType => typeof(T);

    T Latest { get; }

    void Process(T data);

    ILiveTimingDataPoint IProcessor.GetLatestData() => Latest;
}
