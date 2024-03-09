namespace OpenF1.Data;

public interface IProcessor
{
}

public interface IProcessor<T> : IProcessor
{
    void Process(T data);
}
