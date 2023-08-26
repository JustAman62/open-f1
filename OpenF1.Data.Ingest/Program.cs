using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

WriteCommandHelp();

Console.WriteLine("Starting Live Timing Ingestion!");

// Set up some services
var services = new ServiceCollection()
    .AddLogging(builder => builder
        .ClearProviders()
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss.fff] ";
        }))
    .AddLiveTimingDbContext()
    .AddLiveTimingClient()
    .AddLiveTimingProvider()
    .BuildServiceProvider();

var timingClient = services
    .GetRequiredService<ILiveTimingClient>();

// Init the Db if it doesn't exist
await InitDatabase();

// Begin ingesting data to DB
await timingClient
    .StartAsync(IngestData);

void IngestData(string data)
{
    var serviceScope = services.CreateScope();
    var dbContext = serviceScope
        .ServiceProvider
        .GetRequiredService<LiveTimingDbContext>();

    dbContext.Add<RawTimingDataPoint>(new()
    {
        EventData = data,
        LoggedDateTime = DateTime.UtcNow
    });
    dbContext.SaveChanges();
}

// Keep the app running, kill on "q" being written to console
// Write "s" to console for a quick status report
while (true)
{
    var input = Console.ReadLine();
    if (input?.Equals("s", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        Console.WriteLine("Getting Status");
        var serviceScope = services.CreateScope();
        var dbContext = serviceScope
            .ServiceProvider
            .GetRequiredService<LiveTimingDbContext>();
        var startOfDay = DateTime.Today;

        var dataPoints = await dbContext
            .RawTimingDataPoints
            .Where(x => x.LoggedDateTime > startOfDay)
            .CountAsync();

        Console.WriteLine($"{dataPoints} ingested for today.");

        var latestDataPoint = await dbContext
            .RawTimingDataPoints
            .OrderBy(x => x.LoggedDateTime)
            .LastOrDefaultAsync();
        Console.WriteLine($"LatestDataPoint: {latestDataPoint}");
    }

    if (input?.Equals("t", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        Console.WriteLine("Adding test data");
        var serviceScope = services.CreateScope();
        var dbContext = serviceScope
            .ServiceProvider
            .GetRequiredService<LiveTimingDbContext>();

        var added = dbContext
            .RawTimingDataPoints
            .Add(new()
            {
                EventData = "{}",
                LoggedDateTime = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Added test data with ID {added.Entity.Id}");
    }

    if (input?.Equals("h", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        WriteCommandHelp();
    }

    if (input?.Equals("q", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        Console.WriteLine("Shutting Down");
        await services.DisposeAsync();
        break;
    }

    await Task.Delay(500);
}

static void WriteCommandHelp()
{
    Console.WriteLine("==============================");
    Console.WriteLine("=== Commands (h to repeat) ===");
    Console.WriteLine("===  t  Add Test Data      ===");
    Console.WriteLine("===  s  Status Report      ===");
    Console.WriteLine("===  q  Quit               ===");
    Console.WriteLine("==============================");
}

async Task InitDatabase()
{
    var serviceScope = services.CreateScope();
    var dbContext = serviceScope
        .ServiceProvider
        .GetRequiredService<LiveTimingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
