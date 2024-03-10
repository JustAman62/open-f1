using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

public class ManageSessionDisplay(
    ITimingService timingService,
    IJsonTimingClient jsonTimingClient,
    ILiveTimingClient liveTimingClient
) : IDisplay
{
    public Screen Screen => Screen.ManageSession;

    public Task<IRenderable> GetContentAsync()
    {
        var table = new Table();

        _ = table.AddColumns("Type", "Data", "Timestamp");
        var queueSnapshot = timingService.GetQueueSnapshot();
        queueSnapshot.Reverse();
        foreach (var (type, data, timestamp) in queueSnapshot)
        {
            _ = table.AddRow(
                type.EscapeMarkup(),
                data?.EscapeMarkup() ?? "",
                timestamp.ToString("s")
            );
        }

        var rows = new Rows(
            new Text(
                $"Simulation Status: {jsonTimingClient.ExecuteTask?.Status.ToString() ?? "No Simulation Running"}"
            ),
            new Text(
                $"Real Client Status: {liveTimingClient.Connection?.State.ToString() ?? "No Connection"}"
            ),
            new Text($"Delay: {timingService.Delay} / Simulation Time: {DateTimeOffset.UtcNow - timingService.Delay:s}"),
            new Text($"Items in Queue: {timingService.GetRemainingWorkItems()}"),
            new Text($"Queue State:"),
            table
        );

        return Task.FromResult<IRenderable>(new Panel(rows).Expand());
    }
}
