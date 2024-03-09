using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingOverviewDisplay(
    RaceControlMessageProcessor raceControlMessages,
    TimingDataProcessor timingData
) : IDisplay
{
    public Screen Screen => Screen.TimingOverview;

    public Task<IRenderable> GetContentAsync()
    {
        var raceControlPanel = GetRaceControlPanel();
        var timingTower = GetTimingTower();

        var layout = new Layout("Root").SplitColumns(
            new Layout("Timing Tower", timingTower),
            new Layout("Race Control Messages", raceControlPanel)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetTimingTower()
    {
        var table = new Table();
        table.AddColumns("Driver", "Interval", "Last Lap Time");
        if (timingData.LatestLiveTimingDataPoint is null)
        {
            return new Text("No Timing");
        }
        foreach (var line in timingData.LatestLiveTimingDataPoint.Lines.OrderBy(x => x.Value.Line))
        {
            table.AddRow(
                line.Key,
                line.Value.IntervalToPositionAhead?.Value ?? "NULL",
                line.Value.LastLapTime?.Value ?? "NULL"
            );
        }

        table.NoBorder();

        return table;
    }

    private IRenderable GetRaceControlPanel()
    {
        var rows = new Rows(
            raceControlMessages
                .RaceControlMessages.Messages.OrderByDescending(x => x.Value.Utc)
                .Select(x => new Text($"{x.Value.Utc} {x.Value.Message}"))
                .Take(4)
        );
        return new Panel(rows) { Header = new PanelHeader("Race Control Messages"), Expand = true };
    }
}
