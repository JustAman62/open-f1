using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

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
    .AddSessionProvider()
    .AddLiveTimingProvider()
    .BuildServiceProvider();


// Output the current session name
var sessionProvider = services.GetRequiredService<ISessionProvider>();
var sessionName = await sessionProvider.GetSessionName().ConfigureAwait(false);
Console.WriteLine($"Current Session: {sessionName}");

// Start the live timing data processors
var processingService = services
    .GetRequiredService<ProcessingService>();
await processingService.StartAsync();

var timingProvider = services
    .GetRequiredService<ILiveTimingProvider>();

timingProvider.Start();

// Keep the app running, kill on "q" being written to console
// Write "s" to console for a quick status report
while (true)
{
    var input = Console.ReadLine();

    if (input?.Equals("q", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        Console.WriteLine("Shutting Down");
        await services.DisposeAsync();
        break;
    }

    await Task.Delay(500);
}
