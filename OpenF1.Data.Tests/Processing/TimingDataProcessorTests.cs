using AutoMapper;
using AutoMapper.EquivalencyExpression;
using AutoMapper.Execution;
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
                    ["1"] = new()
                    {
                        InPit = false,
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
        Assert.NotNull(processor.LatestLiveTimingDataPoint);
        Assert.NotEmpty(processor.LatestLiveTimingDataPoint.Lines);
        var line = processor.LatestLiveTimingDataPoint.Lines["1"];
        Assert.Equal(1, line.Line);
        Assert.False(line.InPit);
        Assert.Equal("+1.000", line.GapToLeader);
        Assert.Equal("1.11", line.BestLapTime.Value);
    }
}
