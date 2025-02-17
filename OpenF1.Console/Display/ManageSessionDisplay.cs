using OpenF1.Console;
using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

public class ManageSessionDisplay(
    ITimingService timingService,
    IDateTimeProvider dateTimeProvider,
    IJsonTimingClient jsonTimingClient,
    ILiveTimingClient liveTimingClient,
    SessionInfoProcessor sessionInfo
) : IDisplay
{
    public Screen Screen => Screen.ManageSession;

    public Task<IRenderable> GetContentAsync()
    {
        var table = new Table { Title = new TableTitle("Recently Processed Messages") };
        _ = table.AddColumns("Type", "Data", "Timestamp");
        table.Expand();

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

        var status = new Rows(
            new Text(
                $"Simulation Status: {jsonTimingClient.ExecuteTask?.Status.ToString() ?? "No Simulation Running"}"
            ),
            new Text(
                $"Real Client Status: {liveTimingClient.Connection?.State.ToString() ?? "No Connection"}"
            ),
            new Text(
                $"Delay: {dateTimeProvider.Delay} / Simulation Time (UTC): {dateTimeProvider.Utc:s}"
            ),
            new Text($"Items in Queue: {timingService.GetRemainingWorkItems()}")
        );

        var session = new Rows(
            new Text(
                $"Location: {sessionInfo.Latest.Meeting?.Circuit?.ShortName ?? ""}"
            ).RightJustified(),
            new Text($"Type: {sessionInfo.Latest.Name ?? ""}").RightJustified(),
            new Text($"Start (UTC): {sessionInfo.Latest.GetStartDateTimeUtc():s}").RightJustified(),
            new Text($"Key: {sessionInfo.Latest.Key.ToString() ?? ""}").RightJustified()
        );

        session.Collapse();

        var layout = new Layout().SplitRows(
            new Layout("Header")
                .SplitColumns(
                    new Layout("Status", status).Size(76),
                    new Layout("SessionInfo", session)
                )
                .Size(5),
            new Layout("Data Queue", table)
        );

        return Task.FromResult<IRenderable>(layout);
    }
}
