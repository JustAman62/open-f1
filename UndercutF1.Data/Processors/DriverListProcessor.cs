namespace UndercutF1.Data;

public class DriverListProcessor(IMerger merger) : ProcessorBase<DriverListDataPoint>(merger)
{
    public bool IsSelected(string driverNumber) =>
        Latest.GetValueOrDefault(driverNumber)?.IsSelected ?? true;
}
