using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OpenF1.Data;

public class JsonTimingClient(IEnumerable<IProcessor> processors, ILogger<JsonTimingClient> logger)
    : TimingClient(processors, logger),
        IJsonTimingClient
{
    private string _fileName = "";
    private CancellationTokenSource _cts = new();

    public Task? ExecuteTask { get; private set; }

    public IEnumerable<string> GetFileNames() => Directory.GetFiles("./SimulationData");

    public Task StartAsync(string fileName)
    {
        _fileName = fileName;
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        ExecuteTask = Task.Run(() => ExecuteAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _cts.Cancel();
        ExecuteTask = null;
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var lines = File.ReadLinesAsync(_fileName, cancellationToken);

        await foreach (var line in lines)
        {
            try
            {
                var parts = JsonNode.Parse(line)!.AsArray();
                ProcessData(parts[0].ToString(), parts[1].ToString(), DateTimeOffset.Parse(parts[2].ToString()));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to handle data: {line}");
            }
        }
    }
}
