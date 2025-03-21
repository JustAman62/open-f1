namespace OpenF1.Data.Tests;

public class TimingDataPointExtensionsUnitTests
{
    public static TheoryData<string, decimal?> IntervalTestData =>
        new()
        {
            { "LAP 10", 0 },
            { "+1", 1 },
            { "+1.123", (decimal?)1.123 },
            { "1L", null },
        };

    [Theory]
    [MemberData(nameof(IntervalTestData))]
    public void VerifyGapToLeaderSeconds(string input, decimal? output)
    {
        var driver = new TimingDataPoint.Driver() { GapToLeader = input };
        Assert.Equal(output, driver.GapToLeaderSeconds());
    }

    [Theory]
    [MemberData(nameof(IntervalTestData))]
    public void VerifyIntervalSeconds(string input, decimal? output)
    {
        var driver = new TimingDataPoint.Driver.Interval() { Value = input };
        Assert.Equal(output, driver.IntervalSeconds());
    }

    public static TheoryData<string, TimeSpan> LapSectorTimeTestData =>
        new()
        {
            {
                "1:23.456",
                new TimeSpan(days: 0, hours: 0, minutes: 1, seconds: 23, milliseconds: 456)
            },
            {
                "23.456",
                new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 23, milliseconds: 456)
            },
        };

    [Theory]
    [MemberData(nameof(LapSectorTimeTestData))]
    public void VerifyLapSectorTimeToTimeSpan(string input, TimeSpan expected)
    {
        // Arrange
        var lapSectorTime = new TimingDataPoint.Driver.LapSectorTime { Value = input };

        // Act
        var result = lapSectorTime.ToTimeSpan();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(LapSectorTimeTestData))]
    public void VerifyBestLapToTimeSpan(string input, TimeSpan expected)
    {
        // Arrange
        var lapSectorTime = new TimingDataPoint.Driver.BestLap { Value = input };

        // Act
        var result = lapSectorTime.ToTimeSpan();

        // Assert
        Assert.Equal(expected, result);
    }
}
