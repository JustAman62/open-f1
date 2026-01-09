using System.Text.Json;
using NSubstitute;

namespace UndercutF1.Data.Tests;

public class ExtrapolatedClockProcessorTests
{
    public static TheoryData<string, DateTimeOffset, TimeSpan> TestData =>
        new()
        {
            {
                @"{ ""Utc"": ""2024-05-04T20:00:00.998Z"", ""Remaining"": ""00:17:59"", ""Extrapolating"": true }",
                DateTimeOffset.Parse("2024-05-04T20:00:01.998Z"),
                new TimeSpan(0, 17, 58)
            },
            {
                @"{ ""Utc"": ""2024-05-04T20:00:00.998Z"", ""Remaining"": ""00:17:59"", ""Extrapolating"": false }",
                DateTimeOffset.Parse("2024-05-04T20:00:00.998Z"),
                new TimeSpan(0, 17, 59)
            },
            {
                @"{ ""Utc"": ""2024-05-04T20:00:00.998Z"", ""Remaining"": ""00:17:59"", ""Extrapolating"": true }",
                DateTimeOffset.Parse("2024-05-04T20:05:09.998Z"),
                new TimeSpan(0, 12, 50)
            },
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public void VerifyExtrapolatedRemaining(
        string json,
        DateTimeOffset clockTime,
        TimeSpan expected
    )
    {
        // Arrange
        var data = JsonSerializer.Deserialize(
            json,
            TimingDataSerializerContext.Raw.ExtrapolatedClockDataPoint
        )!;
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Utc.Returns(clockTime);

        var processor = new ExtrapolatedClockProcessor(dateTimeProvider);

        // Act
        processor.Process(data);

        // Arrange
        Assert.Equal(expected, processor.ExtrapolatedRemaining());
    }
}
