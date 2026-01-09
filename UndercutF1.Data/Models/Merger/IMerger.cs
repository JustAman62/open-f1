namespace UndercutF1.Data;

public interface IMerger
{
    T Merge<T>(T source, T destination);
}
