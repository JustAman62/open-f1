namespace OpenF1.Data;

public interface IProcessor
{
    LiveTimingDataType LiveTimingDataType { get; }
    Task StartAsync();
}

