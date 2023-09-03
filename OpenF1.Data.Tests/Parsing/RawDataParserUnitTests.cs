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
        var result = parser.ParseRawTimingDataPoint(rawData);

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
            SessionName = "Test Session",
            LoggedDateTime = DateTime.UtcNow
        };
        var parser = new RawDataParser(Substitute.For<ILogger<RawDataParser>>());

        // Act
        var result = parser.ParseRawTimingDataPoint(rawData);

        // Assert
        Assert.NotNull(result);
        var heartbeat = Assert.IsType<HeartbeatDataPoint>(result);
        Assert.NotEqual(DateTimeOffset.MinValue, heartbeat.Data.Utc);
        Assert.Equal(DateTimeOffset.Parse("2023-08-27T15:30:41.8907666Z"), heartbeat.Data.Utc);
        Assert.Equal("Test Session", heartbeat.SessionName);
    }

    [Fact]
    public void VerifyParseTimingDataWithUnusedMembers()
    {
        // Arrange
        var rawData = new RawTimingDataPoint
        {
            EventType = LiveTimingDataType.TimingData.ToString(),
            EventData = "{\"Lines\":{\"1\":{\"Sectors\":{\"1\":{\"Segments\":{\"3\":{\"Status\":2048}}}}}}}",
            LoggedDateTime = DateTime.UtcNow
        };
        var parser = new RawDataParser(Substitute.For<ILogger<RawDataParser>>());

        // Act
        var result = parser.ParseRawTimingDataPoint(rawData);

        // Assert
        Assert.NotNull(result);
    }
}

