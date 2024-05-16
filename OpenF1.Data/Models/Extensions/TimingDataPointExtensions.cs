using System.Globalization;

namespace OpenF1.Data;

public static class TimingDataPointExtensions
{
    public static Dictionary<string, TimingDataPoint.Driver> GetOrderedLines(
        this TimingDataPoint data
    ) => data.Lines.OrderBy(x => x.Value.Line).ToDictionary(x => x.Key, x => x.Value);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    public static decimal? GapToLeaderSeconds(this TimingDataPoint.Driver driver)
    {
        if (driver.GapToLeader?.Contains("LAP") ?? false)
            return 0;

        return decimal.TryParse(driver.GapToLeader, out var seconds) ? seconds : null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    public static decimal? IntervalSeconds(this TimingDataPoint.Driver.Interval interval)
    {
        if (interval.Value?.Contains("LAP") ?? false)
            return 0;

        return decimal.TryParse(interval?.Value, out var seconds) ? seconds : null;
    }

    public static TimeSpan? ToTimeSpan(this TimingDataPoint.Driver.BestLap lap) =>
        TimeSpan.TryParseExact(
            lap.Value,
            ["m\\:ss\\.fff", "ss\\.fff"],
            CultureInfo.InvariantCulture,
            out var result
        )
            ? result
            : null;

    public static TimeSpan? ToTimeSpan(this TimingDataPoint.Driver.LapSectorTime lap) =>
        TimeSpan.TryParseExact(
            lap.Value,
            ["m\\:ss\\.fff", "ss\\.fff"],
            CultureInfo.InvariantCulture,
            out var result
        )
            ? result
            : null;

    public static bool IsRace(this SessionInfoDataPoint? sessionInfo) =>
        (sessionInfo?.Name?.EndsWith("Race") ?? true)
        || (sessionInfo?.Name?.EndsWith("Sprint") ?? true);
}
