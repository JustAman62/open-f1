namespace OpenF1.Data;

public interface IProcessor { }

public interface IProcessor<T> : IProcessor
{
    public Type InputType => typeof(T);

    public T Latest { get; }

    public void Process(T data);
}
