namespace UndercutF1.Data;

public class DriverListProcessor : IProcessor<DriverListDataPoint>
{
    public DriverListDataPoint Latest { get; private set; } = new();

    public bool IsSelected(string driverNumber) =>
        Latest.GetValueOrDefault(driverNumber)?.IsSelected ?? true;

    public virtual void Process(DriverListDataPoint data) => Latest.MergeWith(data);
}
