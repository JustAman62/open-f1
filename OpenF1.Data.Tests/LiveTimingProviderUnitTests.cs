using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenF1.Data.AutoMapper;

namespace OpenF1.Data.Tests;

public class LiveTimingProviderUnitTests
{
    private readonly IMapper _mapper;
    private ILiveTimingClient _timingClient;
    private readonly LiveTimingDbContext _timingDbContext;
    private readonly LiveTimingProvider _provider;
    private readonly SqliteConnection _connection;

    public LiveTimingProviderUnitTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _mapper = new MapperConfiguration(x => x.AddProfile(new TimingDataPointConfiguration())).CreateMapper();
        _timingClient = Substitute.For<ILiveTimingClient>();
        _timingDbContext = new LiveTimingDbContext(new DbContextOptionsBuilder().UseSqlite(_connection).Options);
        _provider = new LiveTimingProvider
            (
                _timingClient,
                _timingDbContext,
                new RawDataParser(Substitute.For<ILogger<RawDataParser>>()),
                _mapper,
                Substitute.For<ILogger<LiveTimingProvider>>()
            );
    }

    [Fact]
    public void VerifyTimingDataAggregationForSameDriver()
    {
        // Arrange & Act
        _provider.HandleRawData(@"{ ""H"": ""Streaming"", ""M"": ""feed"", ""A"": [ ""TimingData"", { ""Lines"": { ""4"": { ""IntervalToPositionAhead"": { ""Value"": """" }, ""Line"": 8, ""Position"": ""8"" }, ""63"": { ""IntervalToPositionAhead"": { ""Value"": """" }, ""Line"": 7, ""Position"": ""7"" } } }, ""2023-08-27T15:19:28.172Z"" ]}");

        var latest = _provider.LatestLiveTimingDataPoint;
        Assert.NotNull(latest);
        Assert.NotNull(latest.Data.Lines["4"]);
        Assert.Equal(8, latest.Data.Lines["4"].Line);
        Assert.Equal("8", latest.Data.Lines["4"].Position);
        Assert.Null(latest.Data.Lines["4"].GapToLeader);

        Assert.NotNull(latest.Data.Lines["63"]);
        Assert.Equal(7, latest.Data.Lines["63"].Line);
        Assert.Equal("7", latest.Data.Lines["63"].Position);
        Assert.Null(latest.Data.Lines["63"].GapToLeader);

        _provider.HandleRawData(@"{ ""H"": ""Streaming"", ""M"": ""feed"", ""A"": [ ""TimingData"", { ""Lines"": { ""4"": { ""GapToLeader"": ""+1.234"", ""Line"": 7, ""Position"": ""7"" }, ""63"": { ""GapToLeader"": ""+1.234"", ""Line"": 8, ""Position"": ""8"" } } }, ""2023-08-27T15:19:28.172Z"" ] }");
        latest = _provider.LatestLiveTimingDataPoint;
        Assert.NotNull(latest);
        Assert.NotNull(latest.Data.Lines["4"]);
        Assert.Equal(7, latest.Data.Lines["4"].Line);
        Assert.Equal("7", latest.Data.Lines["4"].Position);
        Assert.NotNull(latest.Data.Lines["4"].GapToLeader);
        Assert.Equal("+1.234", latest.Data.Lines["4"].GapToLeader);

        Assert.NotNull(latest.Data.Lines["63"]);
        Assert.Equal(8, latest.Data.Lines["63"].Line);
        Assert.Equal("8", latest.Data.Lines["63"].Position);
        Assert.Equal("+1.234", latest.Data.Lines["63"].GapToLeader);

        _provider.HandleRawData(@"{ ""H"": ""Streaming"", ""M"": ""feed"", ""A"": [ ""TimingData"", { ""Lines"": { ""4"": { ""GapToLeader"": ""+1.345"", ""IntervalToPositionAhead"": { ""Value"": ""+0.793"", ""Catching"": true } } } }, ""2023-08-27T15:19:28.172Z"" ] }");
        latest = _provider.LatestLiveTimingDataPoint;
        Assert.NotNull(latest);
        Assert.NotNull(latest.Data.Lines["4"]);
        Assert.Equal(7, latest.Data.Lines["4"].Line);
        Assert.Equal("7", latest.Data.Lines["4"].Position);
        Assert.Equal("+1.345", latest.Data.Lines["4"].GapToLeader);
        Assert.NotNull(latest.Data.Lines["4"].IntervalToPositionAhead);
        Assert.Equal("+0.793", latest.Data.Lines["4"].IntervalToPositionAhead!.Value);
        Assert.True(latest.Data.Lines["4"].IntervalToPositionAhead!.Catching);

        Assert.NotNull(latest.Data.Lines["63"]);
        Assert.Equal(8, latest.Data.Lines["63"].Line);
        Assert.Equal("8", latest.Data.Lines["63"].Position);
        Assert.Equal("+1.234", latest.Data.Lines["63"].GapToLeader);
    }

    [Fact]
    public void VerifyTimingDataSubscription()
    {
        // Arrange
        var receivedDataPoint = default(LiveTimingDataPoint);
        _provider.Subscribe(LiveTimingDataType.TimingData, x => receivedDataPoint = x);
        // Act
        _provider.HandleRawData(@"{ ""H"": ""Streaming"", ""M"": ""feed"", ""A"": [ ""TimingData"", { ""Lines"": { ""4"": { ""IntervalToPositionAhead"": { ""Value"": """" }, ""Line"": 8, ""Position"": ""8"" }, ""63"": { ""IntervalToPositionAhead"": { ""Value"": """" }, ""Line"": 7, ""Position"": ""7"" } } }, ""2023-08-27T15:19:28.172Z"" ]}");

        SpinWait.SpinUntil(() => receivedDataPoint is not null, 1000);

        Assert.NotNull(receivedDataPoint);
        var receivedTimingDataPoint = Assert.IsType<TimingDataPoint>(receivedDataPoint);
        Assert.NotNull(receivedTimingDataPoint.Data.Lines["4"]);
        Assert.Equal(8, receivedTimingDataPoint.Data.Lines["4"].Line);
        Assert.Equal("8", receivedTimingDataPoint.Data.Lines["4"].Position);
        Assert.Null(receivedTimingDataPoint.Data.Lines["4"].GapToLeader);
    }

    [Fact]
    public void VerifyTimingDataWithUnusedMembers()
    {
        // Arrange & Act
        var eventCount = 0;
        _provider.SubscribeRaw((data) => eventCount++);
        var dataPoint = new RawTimingDataPoint
        {
            EventType = LiveTimingDataType.TimingData.ToString(),
            EventData = @"{""Lines"":{""1"":{""Sectors"":{""1"":{""Segments"":{""3"":{""Status"":2048}}}}}}}",
            LoggedDateTime = DateTime.UtcNow
        };
        _provider.HandleRawData(dataPoint);
        var latest = _provider.LatestLiveTimingDataPoint;

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task VerifyTimingDataIntegrationTestAsync()
    {
        // Arrange
        var dataFile = File
            .ReadAllLines("./TimingData.json")
            .Reverse()
            .Select(x => new RawTimingDataPoint
            {
                EventType = LiveTimingDataType.TimingData.ToString(),
                EventData = x.Replace("\"\"", "\"").Trim('"'),
                LoggedDateTime = DateTime.UtcNow
            })
            .ToList();

        await _timingDbContext.Database.EnsureCreatedAsync();
        _timingDbContext.AddRange(dataFile);
        await _timingDbContext.SaveChangesAsync();

        var eventCount = 0;

        // Act
        _provider.SubscribeRaw((data) => eventCount++);
        _provider.Start(DataSource.Recorded);

        SpinWait.SpinUntil(() => eventCount == dataFile.Count, 5000);

        // Assert
        Assert.NotNull(_provider.LatestLiveTimingDataPoint);
        // Only 18 drivers in this recording
        Assert.Equal(20, _provider.LatestLiveTimingDataPoint.Data.Lines.Count);
        // 1, 16, 2 (VER, LEC, SAR) don't have enough data in the sample to have a position,
        // as their position didn't change in the sample window
        Assert.All(_provider.LatestLiveTimingDataPoint.Data.Lines.Where(x => x.Key is not "1" and not "16" and not "2"), x => Assert.NotNull(x.Value.Position));
    }
}
