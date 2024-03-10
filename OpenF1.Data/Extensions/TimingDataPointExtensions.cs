namespace OpenF1.Data;

public static class TimingDataPointExtensions
{
    public static Dictionary<string, TimingDataPoint.Driver> GetOrderedLines(
        this TimingDataPoint data
    ) => data.Lines.OrderBy(x => x.Value.Line).ToDictionary(x => x.Key, x => x.Value);
}
