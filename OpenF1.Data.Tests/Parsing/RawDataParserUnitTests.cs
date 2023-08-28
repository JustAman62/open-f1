using System;
using NSubstitute;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data.Tests;

public class RawDataParserUnitTests
{
    [Theory]
    [InlineData("Unknown")]
    [InlineData("Unknown.z")]
    [InlineData("Unknown.zz")]
    [InlineData("999")]
    public void VerifyThrowsExceptionOnInvalidEventType(string eventType)
    {
        // Arrange
        var rawData = new RawTimingDataPoint
        {
            EventType = eventType
        };
        var parser = new RawDataParser(Substitute.For<ILogger<RawDataParser>>());

        // Act
        var result = parser.ParseRawData(rawData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void VerifyParseHeartbeat()
    {
        // Arrange
        var rawData = new RawTimingDataPoint
        {
            EventType = LiveTimingDataType.Heartbeat.ToString(),
            EventData = "{\"Utc\":\"2023-08-27T15:30:41.8907666Z\",\"_kf\":true}",
            LoggedDateTime = DateTime.UtcNow
        };
        var parser = new RawDataParser(Substitute.For<ILogger<RawDataParser>>());

        // Act
        var result = parser.ParseRawData(rawData);

        // Assert
        Assert.NotNull(result);
        var heartbeat = Assert.IsType<HeartbeatDataPoint>(result);
        Assert.NotEqual(DateTimeOffset.MinValue, heartbeat.Data.Utc);
        Assert.Equal(DateTimeOffset.Parse("2023-08-27T15:30:41.8907666Z"), heartbeat.Data.Utc);
    }
}

