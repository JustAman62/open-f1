using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UndercutF1.Data.Tests;

public class SessionInfoProcessorTests
{
    [Fact]
    public void VerifyDataUpdate()
    {
        // Arrange
        var processor = new SessionInfoProcessor(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<ILogger<SessionInfoProcessor>>()
        );

        var data = new List<SessionInfoDataPoint>()
        {
            new() { Type = "Race" },
            new() { Key = 1234 },
        };

        // Act
        foreach (var dataPoint in data)
        {
            processor.Process(dataPoint);
        }

        // Assert
        Assert.NotNull(processor.Latest);
        Assert.Equal("Race", processor.Latest.Type);
        Assert.Equal(1234, processor.Latest.Key);
    }
}
