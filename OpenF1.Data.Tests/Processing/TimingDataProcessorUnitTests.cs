using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data.Tests;

public class TimingDataProcessorUnitTests
{
    [Fact]
    public async Task VerifyUpdateForSameDriver()
    {
        var (processor, timingProvider) = CreateProcessor();
        await processor.StartAsync();

        timingProvider.TimingDataReceived += Raise.Event<EventHandler<TimingDataPoint>>(
            new object(),
            new TimingDataPoint(new()
            {
                Lines = new()
                {
                    ["1"] = new()
                    {
                        GapToLeader = "+1.123",
                        IntervalToPositionAhead = new() { Value = "+0.123" }
                    }
                }
            },
            DateTime.UtcNow));

        Assert.NotEmpty(processor.DriverLapData);
        Assert.True(processor.DriverLapData.TryGetValue("1", out var lapDriverData));
        Assert.True(lapDriverData.TryGetValue(0, out var driverData));
        Assert.Equal("+1.123", driverData.GapToLeader);
        Assert.Equal("+0.123", driverData.IntervalToPositionAhead!.Value);

        timingProvider.TimingDataReceived += Raise.Event<EventHandler<TimingDataPoint>>(
            new object(),
            new TimingDataPoint(new()
            {
                Lines = new()
                {
                    ["1"] = new()
                    {
                        GapToLeader = "+2.345",
                        IntervalToPositionAhead = new() { Value = "+0.234" }
                    }
                }
            },
            DateTime.UtcNow));

        Assert.NotEmpty(processor.DriverLapData);
        Assert.True(processor.DriverLapData.TryGetValue("1", out lapDriverData));
        Assert.True(lapDriverData.TryGetValue(0, out driverData));
        Assert.Equal("+2.345", driverData.GapToLeader);
        Assert.Equal("+0.234", driverData.IntervalToPositionAhead!.Value);
    }

    [Fact]
    public async Task VerifyLapChangeForDriver()
    {
        var (processor, timingProvider) = CreateProcessor();
        await processor.StartAsync();

        timingProvider.TimingDataReceived += Raise.Event<EventHandler<TimingDataPoint>>(
            new object(),
            new TimingDataPoint(
                new()
                {
                    Lines = new()
                    {
                        ["1"] = new()
                        {
                            NumberOfLaps = 2,
                            LastLapTime = new() { Value = "1:10:00.000" }
                        }
                    }
                },
                DateTime.UtcNow)
            );

        timingProvider.TimingDataReceived += Raise.Event<EventHandler<TimingDataPoint>>(
            new object(),
            new TimingDataPoint(
                new()
                {
                    Lines = new()
                    {
                        ["1"] = new()
                        {
                            NumberOfLaps = 3,
                            LastLapTime = new() { Value = "1:12:00.000" }
                        }
                    }
                },
                DateTime.UtcNow)
            );

        Assert.NotEmpty(processor.DriverLapData);
        Assert.True(processor.DriverLapData.TryGetValue("1", out var lapDriverData));

        Assert.True(lapDriverData.TryGetValue(2, out var lap2Data));
        Assert.Equal(2, lap2Data.NumberOfLaps);
        Assert.Equal("1:10:00.000", lap2Data.LastLapTime!.Value);

        Assert.True(lapDriverData.TryGetValue(3, out var lap3Data));
        Assert.Equal(3, lap3Data.NumberOfLaps);
        Assert.Equal("1:12:00.000", lap3Data.LastLapTime!.Value);
    }

    private (TimingDataProcessor, ILiveTimingProvider) CreateProcessor()
    {
        var mapper = new MapperConfiguration(x =>
                x.AddMaps(typeof(AutoMapper.TimingDataPointConfiguration).Assembly))
            .CreateMapper();

        var liveTimingProvider = Substitute.For<ILiveTimingProvider>();

        var processor = new TimingDataProcessor(
            liveTimingProvider,
            mapper,
            Substitute.For<ILogger<TimingDataProcessor>>());
        return (processor, liveTimingProvider);
    }
}

