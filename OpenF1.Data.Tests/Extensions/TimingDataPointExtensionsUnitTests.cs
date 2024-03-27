namespace OpenF1.Data.Tests;

public class TimingDataPointExtensionsUnitTests
{
    public static TheoryData<string, decimal?> TestData =>
        new()
        {
            { "LAP 10", null },
            { "+1", 1 },
            { "+1.123", (decimal?)1.123 },
            { "1L", null },
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public void VerifyGapToLeaderSeconds(string input, decimal? output)
    {
        var driver = new TimingDataPoint.Driver() { GapToLeader = input };
        Assert.Equal(output, driver.GapToLeaderSeconds());
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public void VerifyIntervalSeconds(string input, decimal? output)
    {
        var driver = new TimingDataPoint.Driver.Interval() { Value = input };
        Assert.Equal(output, driver.IntervalSeconds());
    }
}
