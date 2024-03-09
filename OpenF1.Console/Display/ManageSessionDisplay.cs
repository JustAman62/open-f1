using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

public class ManageSessionDisplay(IJsonTimingClient jsonTimingClient, ILiveTimingClient liveTimingClient) : IDisplay
{
    public Screen Screen => Screen.ManageSession;

    public Task<IRenderable> GetContentAsync()
    {
        var table = new Table();
        table.AddColumn("Recent Data Points");
        foreach (var item in liveTimingClient.RecentDataPoints)
        {
            table.AddRow(item.EscapeMarkup());
        }

        var rows = new Rows(
            new Text($"Simulation Status: {jsonTimingClient.ExecuteTask?.Status.ToString() ?? "No Simulation Running"}"),
            new Text($"Real Client Status: {liveTimingClient.Connection?.State.ToString() ?? "No Connection"}"),
            table
        );

        return Task.FromResult<IRenderable>(new Panel(rows).Expand());
    }
}
