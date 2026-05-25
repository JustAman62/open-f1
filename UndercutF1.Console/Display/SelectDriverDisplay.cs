using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class SelectDriverDisplay(
    DriverListProcessor driverList,
    TimingDataProcessor timingData,
    SessionInfoProcessor sessionInfo,
    State state
) : IDisplay
{
    public Screen Screen => Screen.SelectDriver;

    public Task<IRenderable> GetContentAsync()
    {
        var driverTower = GetDriverTower();
        var panel = new Panel(driverTower).Padding(2, 4).Expand();
        panel.Header = new PanelHeader("Show/Hide Drivers");
        var layout = new Layout("Content", panel);
        return Task.FromResult<IRenderable>(layout);
    }

    private Table GetDriverTower()
    {
        var table = new Table();
        table
            .AddColumns(
                new TableColumn("Drivers") { Width = 8 },
                new TableColumn("Gap") { Width = 8, Alignment = Justify.Right },
                new TableColumn("Action  ") { Alignment = Justify.Right }
            )
            .NoBorder()
            .NoSafeBorder()
            .RemoveColumnPadding();

        var comparisonDataPoint = timingData.Latest.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );

        var lines = timingData.Latest.GetOrderedLines();

        foreach (var (driverNumber, line) in lines)
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var isComparisonLine = line == comparisonDataPoint.Value;

            var driverTag = DisplayUtils.MarkedUpDriverNumber(driver);
            var decoration = Decoration.None;
            if (!driver.IsSelected)
            {
                driverTag = $"[dim]{driverTag}[/]";
                decoration |= Decoration.Dim;
            }

            driverTag = state.CursorOffset == line.Line ? $">{driverTag}<" : $" {driverTag} ";
            var actionText = driver.IsSelected ? "Deselect" : "  Select";
            actionText = state.CursorOffset == line.Line ? $" >{actionText}<" : $"  {actionText} ";

            if (sessionInfo.Latest.IsRace())
            {
                table.AddRow(
                    new Markup(driverTag),
                    state.CursorOffset > 0
                        ? DisplayUtils.GetGapBetweenLines(
                            lines,
                            comparisonDataPoint.Key,
                            driverNumber,
                            decoration
                        )
                        : new Text(
                            $"{line.IntervalToPositionAhead?.Value}".ToFixedWidth(6),
                            DisplayUtils.GetStyle(
                                line.IntervalToPositionAhead,
                                false,
                                decoration: decoration
                            )
                        ),
                    new Text(actionText)
                );
            }
            else
            {
                var bestDriver = timingData.Latest.GetOrderedLines().First();
                var gapToLeader = (
                    line.BestLapTime.ToTimeSpan() - bestDriver.Value.BestLapTime.ToTimeSpan()
                )?.TotalSeconds;

                table.AddRow(
                    new Markup(driverTag),
                    new Text(
                        $"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".ToFixedWidth(7),
                        DisplayUtils.STYLE_NORMAL.Combine(new Style(decoration: decoration))
                    )
                );
            }
        }

        return table;
    }
}
