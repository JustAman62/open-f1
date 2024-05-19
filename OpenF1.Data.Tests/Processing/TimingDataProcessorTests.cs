using AutoMapper;
using AutoMapper.EquivalencyExpression;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenF1.Data.AutoMapper;

namespace OpenF1.Data.Tests;

public class TimingDataProcessorTests
{
    [Fact]
    public void VerifyDataUpdate()
    {
        // Arrange
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddCollectionMappers();
            cfg.AddProfile<TimingDataPointConfiguration>();
        }).CreateMapper();
        var processor = new TimingDataProcessor(mapper);

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
                        BestLapTime = new() { Value = "1.11" }
                    }
                }
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new() { InPit = false, }
                }
            }
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
    }

    [Fact]
    public void VerifyBestLapUpdatesOnImprovement()
    {
        // Arrange
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddCollectionMappers();
            cfg.AddProfile<TimingDataPointConfiguration>();
        }).CreateMapper();
        var processor = new TimingDataProcessor(mapper);

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
                        BestLapTime = new() { Value = initialBestLapTime }
                    }
                }
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        NumberOfLaps = 2,
                        BestLapTime = new() { Value = fasterBestLapTime }
                    }
                }
            }
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
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddCollectionMappers();
            cfg.AddProfile<TimingDataPointConfiguration>();
        }).CreateMapper();
        var processor = new TimingDataProcessor(mapper);

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
                        BestLapTime = new() { Value = initialBestLapTime }
                    }
                }
            },
            new()
            {
                Lines = new Dictionary<string, TimingDataPoint.Driver>()
                {
                    ["1"] = new()
                    {
                        NumberOfLaps = 2,
                        BestLapTime = new() { Value = slowerBestLapTime }
                    }
                }
            }
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
