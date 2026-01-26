namespace UndercutF1.Data.Tests;

public class TimingDataProcessorTests
{
    [Fact]
    public void VerifyDataUpdate()
    {
        // Arrange
        var processor = new TimingDataProcessor();

        var data = new List<TimingDataPoint>()
        {
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        Line = 1,
                        GapToLeader = "+1.000",
                        InPit = true,
                        BestLapTime = new() { Value = "1.11" },
                        Sectors = new() { ["1"] = new() { Value = "10.123" } },
                    },
                },
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new() { InPit = false },
                },
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new() { Sectors = new() { ["2"] = new() { Value = "20.234" } } },
                },
            },
        };

        // Act
        foreach (var dataPoint in data)
        {
            processor.Process(dataPoint);
        }

        // Assert
        Assert.NotNull(processor.Latest);
        Assert.NotEmpty(processor.Latest.Lines);
        var line = processor.Latest.Lines["1"];
        Assert.Equal(1, line.Line);
        Assert.False(line.InPit);
        Assert.Equal("+1.000", line.GapToLeader);
        Assert.Equal("1.11", line.BestLapTime.Value);
        Assert.Equal("10.123", line.Sectors["1"].Value);
        Assert.Equal("20.234", line.Sectors["2"].Value);
    }

    [Fact]
    public void VerifyBestLapUpdatesOnImprovement()
    {
        // Arrange
        var processor = new TimingDataProcessor();

        var initialBestLapTime = "1:34.678";
        var fasterBestLapTime = "1:20.123";

        var data = new List<TimingDataPoint>()
        {
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        Line = 1,
                        NumberOfLaps = 1,
                        BestLapTime = new() { Value = initialBestLapTime },
                    },
                },
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        NumberOfLaps = 2,
                        BestLapTime = new() { Value = fasterBestLapTime },
                    },
                },
            },
        };

        // Act
        foreach (var dataPoint in data)
        {
            processor.Process(dataPoint);
        }

        // Assert
        Assert.NotNull(processor.Latest);
        Assert.NotEmpty(processor.Latest.Lines);

        var line = processor.Latest.Lines["1"];
        Assert.Equal(1, line.Line);
        Assert.Equal(fasterBestLapTime, line.BestLapTime.Value);

        Assert.Equal(fasterBestLapTime, processor.BestLaps["1"].BestLapTime.Value);
    }

    [Fact]
    public void VerifyBestLapDoesNotUpdateOnSlowerLap()
    {
        // Arrange
        var processor = new TimingDataProcessor();

        var initialBestLapTime = "1:34.678";
        var slowerBestLapTime = "1:50.123";

        var data = new List<TimingDataPoint>()
        {
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        Line = 1,
                        NumberOfLaps = 1,
                        BestLapTime = new() { Value = initialBestLapTime },
                    },
                },
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        NumberOfLaps = 2,
                        BestLapTime = new() { Value = slowerBestLapTime },
                    },
                },
            },
        };

        // Act
        foreach (var dataPoint in data)
        {
            processor.Process(dataPoint);
        }

        // Assert
        Assert.NotNull(processor.Latest);
        Assert.NotEmpty(processor.Latest.Lines);

        var line = processor.Latest.Lines["1"];
        Assert.Equal(1, line.Line);
        Assert.Equal(slowerBestLapTime, line.BestLapTime.Value);

        Assert.Equal(initialBestLapTime, processor.BestLaps["1"].BestLapTime.Value);
    }
}
