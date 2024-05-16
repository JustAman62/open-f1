using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingHistoryDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    LapCountProcessor lapCountProcessor
) : IDisplay
{
    public Screen Screen => Screen.TimingHistory;

    private readonly Style _personalBest =
        new(foreground: Color.White, background: new Color(0, 118, 0));
    private readonly Style _overallBest =
        new(foreground: Color.White, background: new Color(118, 0, 118));
    private Style _normal = new(foreground: Color.White);

    public Task<IRenderable> GetContentAsync()
    {
        var timingTower = GetTimingTower();

        var layout = new Layout("Root").SplitRows(new Layout("Timing Tower", timingTower));

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetTimingTower()
    {
        var selectedLapNumber = state.CursorOffset + 1;
        var selectedLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber);
        var previousLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber - 1);

        if (selectedLapDrivers is null)
            return new Text($"No Data for Lap {selectedLapNumber}");

        var table = new Table();
        table.AddColumns(
            $"LAP {selectedLapNumber, 2}/{lapCountProcessor.Latest?.TotalLaps}",
            "Gap",
            "Interval",
            "Last Lap",
            "S1",
            "S2",
            "S3",
            " "
        );
        table.SimpleBorder();
        table.RemoveColumnPadding();

        foreach (var (driverNumber, line) in selectedLapDrivers.OrderBy(x => x.Value.Line))
        {
            var lap = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var previousLap = previousLapDrivers?.GetValueOrDefault(driverNumber) ?? new();
            var teamColour = lap.TeamColour ?? "000000";

            var positionChange = line.Line - previousLap.Line;

            table.AddRow(
                new Markup(
                    $"{line.Position, 2} [#{teamColour}]{lap.RacingNumber, 2} {lap.Tla ?? "UNK"}[/]"
                ),
                new Markup($"{line.GapToLeader} {GetMarkedUp(line.GapToLeaderSeconds() - previousLap.GapToLeaderSeconds())}" ?? "", _normal),
                new Markup($"{line.IntervalToPositionAhead?.Value} {GetMarkedUp(line.IntervalToPositionAhead?.IntervalSeconds() - previousLap.IntervalToPositionAhead?.IntervalSeconds())}" ?? "", _normal),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                ),
                new Markup(GetPositionChangeMarkup(positionChange))
            );
        }

        return table;
    }

    private string GetMarkedUp(decimal? time) => time switch
    {
        < 0 => $"[green dim italic]({time})[/]",
        < 0.5m => $"[grey62 dim italic](+{time})[/]",
        null => "",
        _ => $"[yellow dim italic](+{time})[/]",
    };

    private string GetPositionChangeMarkup(int? change) => change switch
    {
        < 0 => "[green]▲[/]",
        > 0 => "[yellow]▼[/]",
        _ => ""
    };

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return _normal;
        if (time.OverallFastest ?? false)
            return _overallBest;
        if (time.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }
}
