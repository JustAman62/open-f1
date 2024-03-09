namespace OpenF1.Data.Tests;

public class RawTimingDataPointUnitTests
{
    [Theory]
    [InlineData("TestEventType", "{\"key\":\"value\"}", "2023-08-27 15:30:38Z", "{\"H\": \"Streaming\",\"M\": \"feed\",\"A\": [\"TestEventType\",{\"key\": \"value\"},\"2023-08-27T15:30:38.926Z\"]}")]
    [InlineData("CompressedEvent", "7ZTNTsMwEITfZc8B7Y/ttX3nDBI9QBGHCvUQobaoDacq707SWOADXR4AXyJF8pfdmXHmDA+HUz/0hz3klzOs+t32NGx2H5CBkeUG4w3rinwWzBJvhTBwlDV0cLcfjv32BPkMND8eh83wOb3C/X513Ly9T0eeIDsNHTxDFkXsYA3Zi44d8HUiuKQXhKgQLBPhrhNMfAHYB1/NIDTWmk/OawWVGjGUUFoQThprxFjMKZYpMdRIuI6oSxfEEaYaicZiWuS7VMtnQz7HuCCRavlsxCJumSJItRYWA/FUkJn9QQzHFr+W3HE+q5aIolujVp8XI0PmkqFPVCHOsipRsYprd50horKqXsx7A0EsU0IdSDDcZaLfbr0ajpErsTtfI9FwTNQtWqYf/xsZx+7vqvCCPrnUqqJVRauKVhVWVYSYvKK2qmhV0ariH1fF6/gF", "2023-08-27 15:30:38Z", "{\"H\": \"Streaming\",\"M\": \"feed\",\"A\": [\"CompressedEvent\",\"7ZTNTsMwEITfZc8B7Y/ttX3nDBI9QBGHCvUQobaoDacq707SWOADXR4AXyJF8pfdmXHmDA+HUz/0hz3klzOs+t32NGx2H5CBkeUG4w3rinwWzBJvhTBwlDV0cLcfjv32BPkMND8eh83wOb3C/X513Ly9T0eeIDsNHTxDFkXsYA3Zi44d8HUiuKQXhKgQLBPhrhNMfAHYB1/NIDTWmk/OawWVGjGUUFoQThprxFjMKZYpMdRIuI6oSxfEEaYaicZiWuS7VMtnQz7HuCCRavlsxCJumSJItRYWA/FUkJn9QQzHFr+W3HE+q5aIolujVp8XI0PmkqFPVCHOsipRsYprd50horKqXsx7A0EsU0IdSDDcZaLfbr0ajpErsTtfI9FwTNQtWqYf/xsZx+7vqvCCPrnUqqJVRauKVhVWVYSYvKK2qmhV0ariH1fF6/gF\",\"2023-08-27T15:30:38.926Z\"]}")]
    public void VerifyParse(string expectedEventType, string expectedData, string expectedDateTime, string input)
    {
        // Arrange & Act
        var result = RawTimingDataPoint.Parse(input, "Session Name");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SessionName", result.SessionName);
        Assert.Equal(expectedEventType, result.EventType);
        Assert.Equal(expectedData, result.EventData);
        Assert.Equal(expectedDateTime, result.LoggedDateTime.ToUniversalTime().ToString("u"));
    }
}
