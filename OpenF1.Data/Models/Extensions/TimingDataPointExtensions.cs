namespace OpenF1.Data;

public static class TimingDataPointExtensions
{
    public static Dictionary<string, TimingDataPoint.Driver> GetOrderedLines(
        this TimingDataPoint data
    ) => data.Lines.OrderBy(x => x.Value.Line).ToDictionary(x => x.Key, x => x.Value);

    public static decimal? GapToLeaderSeconds(this TimingDataPoint.Driver driver) =>
        decimal.TryParse(driver.GapToLeader, out var seconds) ? seconds : null;

    public static decimal? IntervalSeconds(this TimingDataPoint.Driver.Interval interval) =>
        decimal.TryParse(interval?.Value, out var seconds) ? seconds : null;

    public static bool IsRace(this SessionInfoDataPoint? sessionInfo) =>
        (sessionInfo?.Name?.EndsWith("Race") ?? true)
        || (sessionInfo?.Name?.EndsWith("Sprint") ?? true);
}
