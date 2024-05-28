using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class DriverTrackerDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    PositionDataProcessor positionData,
    ILogger<DriverTrackerDisplay> logger
) : IDisplay
{
    public Screen Screen => Screen.DriverTracker;

    public Task<IRenderable> GetContentAsync()
    {
        var driverTower = GetDriverTower();
        var details = GetDetails();
        var layout = new Layout("Root").SplitRows(
            new Layout("Content").SplitColumns(
                new Layout("Driver List", driverTower),
                new Layout("Details", details)
            ),
            new Layout("Info")
        );

        layout["Info"].Size = 6;
        layout["Content"]["Driver List"].Size = 13;

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetDriverTower()
    {
        var table = new Table();
        table.AddColumns("Tracker", " ");
        table.SimpleBorder();
        table.RemoveColumnPadding();

        foreach (var (driverNumber, line) in timingData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var isSelected =
                state.CursorOffset == line.Line || state.SelectedDrivers.Contains(driverNumber);
            table.AddRow(
                DisplayUtils.DriverTag(driver, line, isSelected),
                new Text(
                    " ",
                    state.CursorOffset == line.Line
                        ? DisplayUtils.STYLE_INVERT
                        : DisplayUtils.STYLE_NORMAL
                )
            );
        }

        return table;
    }

    private IRenderable GetDetails()
    {
        var canvas = new Canvas(200, 200);
        foreach (var driver in driverList.Latest)
        {
            var position = positionData
                .Latest.Position.LastOrDefault()
                ?.Entries.GetValueOrDefault(driver.Key);
            if (position is not null && position.X.HasValue && position.Y.HasValue)
            {
                var color = System.Drawing.Color.FromArgb(
                    Convert.ToInt32(driver.Value.TeamColour, 16)
                );

                canvas.SetPixel(
                    (position.X.Value / 100) + 100,
                    (position.Y.Value / 100) + 100,
                    new Color(color.R, color.G, color.B)
                );
            }
        }
        canvas.Scale = true;
        return canvas;
    }
}
