using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public class TyreStintDisplay(
    State state,
    CommonDisplayComponents common,
    PitLaneTimeCollectionProcessor pitLaneTimeCollection,
    PitStopSeriesProcessor pitStopSeries,
    DriverListProcessor driverList,
    TimingAppDataProcessor timingAppData,
    LapCountProcessor lapCount
) : IDisplay
{
    public Screen Screen => Screen.TyreStints;

    public Task<IRenderable> GetContentAsync()
    {
        var pitStintList = GetPitStintList();

        var layout = new Layout("Root").SplitRows(
            new Layout("Pit Stints", pitStintList),
            new Layout("Footer")
                .SplitColumns(
                    new Layout("Status Panel", common.GetStatusPanel()).Size(15),
                    new Layout("Selected Stint Detail", GetStintDetail())
                )
                .Size(6)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private Rows GetPitStintList()
    {
        var rows = new List<IRenderable>
        {
            new Text(
                $"LAP {lapCount.Latest.CurrentLap, 2}/{lapCount.Latest.TotalLaps, 2} Pit Stops"
            ),
        };
        var totalLapCount = lapCount.Latest.TotalLaps.GetValueOrDefault();

        foreach (var (driverNumber, line) in timingAppData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest.GetValueOrDefault(driverNumber) ?? new();
            var rowMarkup = DisplayUtils.MarkedUpDriverNumber(driver);
            rowMarkup = $"{line.Line.ToString()?.ToFixedWidth(2)} {rowMarkup} ";

            var (selectedDriverNumber, _) = timingAppData.Latest.Lines.FirstOrDefault(x =>
                x.Value.Line == state.CursorOffset
            );

            if (selectedDriverNumber == driverNumber)
            {
                rowMarkup = $"[invert]{rowMarkup}[/]";
            }

            var lineTotalPadLength = 0;

            foreach (var (stintNumber, stint) in line.Stints.OrderBy(x => x.Key))
            {
                var lapsOnThisTyre = stint.GetStintDuration();
                var stintLength = Math.Max(1, lapsOnThisTyre);
                lineTotalPadLength += stintLength;
                rowMarkup += DisplayUtils.MarkedUpTyreStint(
                    stint,
                    stintLength,
                    displayFullAge: false
                );
            }

            if (totalLapCount > 0)
            {
                // Add a white cell for the final lap
                var emptyCellsToAdd = Math.Max(0, totalLapCount - lineTotalPadLength);
                var emptyCells = string.Empty.ToFixedWidth(emptyCellsToAdd);
                rowMarkup = rowMarkup + emptyCells + "[white]▞▞[/]";
            }

            rows.Add(new Markup(rowMarkup));
        }

        return new Rows(rows);
    }

    private Columns GetStintDetail()
    {
        var (selectedDriverNumber, line) = timingAppData.Latest.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );
        if (selectedDriverNumber is null)
        {
            return new Columns();
        }

        var columns = new List<Rows>();
        foreach (var (stintNumber, stint) in line.Stints)
        {
            var pitLaneTime = pitLaneTimeCollection
                .Latest.PitTimesList.GetValueOrDefault(selectedDriverNumber)
                ?.ElementAtOrDefault(int.Parse(stintNumber) - 1);
            var pitStop = pitStopSeries
                .Latest.PitTimes.GetValueOrDefault(selectedDriverNumber)
                ?.ElementAtOrDefault(int.Parse(stintNumber) - 1)
                .Value?.PitStop;

            var compoundMarkup = DisplayUtils.GetStyleForTyreCompound(stint.Compound).ToMarkup();
            // Use a consistent tyre compound header to centre it nicely
            var header = stint.Compound switch
            {
                "HARD" => " HARD",
                "MEDIUM" => " MED ",
                "SOFT" => " SOFT",
                "INTERMEDIATE" => " INT ",
                "WET" => " WET ",
                _ => " UNK ",
            };

            header += $"LAP {line.Stints.GetPitLapForStint(stintNumber)} ".PadLeft(9);

            var rows = new List<Markup>
            {
                new($"[{compoundMarkup}]{header}[/]"),
                new(
                    $"Start Age  {(stint.New.GetValueOrDefault() ? "[green]NEW[/]" : $" {stint.StartLaps:D2}")}"
                ),
                new($"Total Laps  {stint.TotalLaps:D2}"),
                new($"Best  {stint.LapTime}"),
                pitLaneTime is null ? new(" ") : new($"Lane {pitLaneTime?.Duration?.PadLeft(9)}"),
                pitStop is null ? new(" ") : new($"Stop {pitStop.PitStopTime?.PadLeft(9)}"),
            };
            columns.Add(new Rows(rows).Collapse());
        }
        return new Columns(columns).Collapse();
    }
}
