using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class JsonTimingClient(ITimingService timingService, ILogger<JsonTimingClient> logger)
    : IJsonTimingClient
{
    private string _directory = "";
    private CancellationTokenSource _cts = new();

    public Task? ExecuteTask { get; private set; }

    /// <inheritdoc />
    public IEnumerable<string> GetDirectoryNames() =>
        Directory
            .GetDirectories("./SimulationData")
            .Where(x =>
                Directory
                    .GetFiles(x)
                    .All(fileName =>
                        fileName.EndsWith("live.txt") || fileName.EndsWith("subscribe.txt")
                    )
            );

    public async Task StartAsync(string directory)
    {
        _directory = directory;
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        ExecuteTask = Task.Run(() => ExecuteAsync(_cts.Token), _cts.Token);
        await timingService.StartAsync();
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        ExecuteTask = null;
        await timingService.StopAsync();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Handle the dump of data we receive at subscription time
        var subscriptionData = await File.ReadAllTextAsync(
                Path.Join(_directory, "/subscribe.txt"),
                cancellationToken
            )
            .ConfigureAwait(false);

        timingService.ProcessSubscriptionData(subscriptionData);

        // Handle the real-time data
        var lines = File.ReadLines(Path.Join(_directory, "/live.txt"));

        // Calculate the delay we need to make this simulation real-time
        var (_, _, firstTimestamp) = ProcessLine(lines.First());
        timingService.Delay = DateTimeOffset.UtcNow - firstTimestamp;
        logger.LogInformation($"Calculated a delay of {timingService.Delay} between now and {firstTimestamp:s}");

        foreach (var line in lines)
        {
            try
            {
                var (type, data, timestamp) = ProcessLine(line);
                timingService.Enqueue(type, data, timestamp);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to handle data: {line}");
            }
        }
    }

    private (string type, string? data, DateTimeOffset timestamp) ProcessLine(string line)
    {
        var data = JsonNode.Parse(line);
        var parts = data?["A"]?.AsArray() ?? data!.AsArray();
        return (
            parts[0]!.ToString(),
            parts[1]!.ToString(),
            DateTimeOffset.Parse(parts[2]!.ToString())
        );
    }
}
