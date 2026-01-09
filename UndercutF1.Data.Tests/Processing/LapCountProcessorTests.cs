namespace UndercutF1.Data.Tests;

public class LapCountProcessorTests
{
    [Fact]
    public void VerifyDataUpdate()
    {
        // Arrange
        var processor = new LapCountProcessor();

        var data = new List<LapCountDataPoint>()
        {
            new() { TotalLaps = 100, CurrentLap = 1 },
            new() { CurrentLap = 2 },
            new() { CurrentLap = 3 },
        };

        // Act
        foreach (var dataPoint in data)
        {
            processor.Process(dataPoint);
        }

        // Assert
        Assert.NotNull(processor.Latest);
        Assert.Equal(3, processor.Latest.CurrentLap);
        Assert.Equal(100, processor.Latest.TotalLaps);
    }
}
