namespace OpenF1.Data;

public interface IProcessor<T>
{
    void Process(T data);
}
