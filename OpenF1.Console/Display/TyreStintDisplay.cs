using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TyreStintDisplay(
    State state,
    TyreStintSeriesProcessor tyreStintSeries,
    DriverListProcessor driverList,
    TimingDataProcessor timingData,
    LapCountProcessor lapCountProcessor
) : IDisplay
{
    private const int DRIVER_COLUMN_LENGTH = 10;

    public Screen Screen => Screen.TyreStints;

    public Task<IRenderable> GetContentAsync()
    {
        var pitStintList = GetPitStintList();

        var layout = new Layout("Root").SplitRows(
            new Layout("Pit Stints", pitStintList),
            new Layout("Stint Detail").Size(6)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private Rows GetPitStintList()
    {
        var rows = new List<IRenderable> { new Text("Driver    Pit Stops") };
        var totalLapCount = lapCountProcessor.Latest.TotalLaps.GetValueOrDefault();

        foreach (var (driverNumber, line) in timingData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var stints = tyreStintSeries.Latest.Stints.GetValueOrDefault(driverNumber) ?? [];
            var rowMarkup = DisplayUtils.MarkedUpDriverNumber(driver);
            rowMarkup = $"{line.Line.ToString()?.ToFixedWidth(2)} {rowMarkup} ";

            var (selectedDriverNumber, _) = timingData.Latest.Lines.FirstOrDefault(x =>
                x.Value.Line == state.CursorOffset
            );

            if (selectedDriverNumber == driverNumber)
            {
                rowMarkup = $"[invert]{rowMarkup}[/]";
            }

            var lineTotalPadLength = 0;

            foreach (var (stintNumber, stint) in stints.OrderBy(x => x.Key))
            {
                var markup = DisplayUtils.GetStyleForTyreCompound(stint.Compound).ToMarkup();
                var lapsOnThisTyre = (stint.TotalLaps - stint.StartLaps).GetValueOrDefault();

                var padLength = Math.Max(1, lapsOnThisTyre - 1);
                var text = $"{lapsOnThisTyre}".ToFixedWidth(padLength);
                if (text.Length == 1)
                {
                    text = string.Empty;
                }
                lineTotalPadLength += text.Length + 1;

                // Prepend the compound indicator, and wrap the whole line in markup to colour it
                rowMarkup += $"[{markup}]{stint.Compound?[0] ?? ' '}{text}[/]";
            }

            // Add a white cell for the final lap
            var emptyCellsToAdd = totalLapCount - lineTotalPadLength;
            var emptyCells = string.Empty.ToFixedWidth(emptyCellsToAdd);
            rowMarkup = rowMarkup + emptyCells + "[white on white] [/]";

            rows.Add(new Markup(rowMarkup));
        }

        return new Rows(rows);
    }
}
