using Microsoft.Extensions.Options;
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
            table.AddRow(
                DisplayUtils.DriverTag(driver, line, state.SelectedDrivers.Contains(driverNumber)),
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
        var height = System.Console.WindowHeight - 8;
        var maxCoordVal = 10000;
        var scaleFactor = maxCoordVal / height;
        var canvas = new Canvas(height + 1, height + 1);
        for (var i = 0; i < height; i++)
        {
            canvas.SetPixel(i, 0, Color.White);
            canvas.SetPixel(0, i, Color.White);
        }
        foreach (var driver in driverList.Latest.Where(x => state.SelectedDrivers.Contains(x.Key)))
        {
            var position = positionData
                .Latest.Position.LastOrDefault()
                ?.Entries.GetValueOrDefault(driver.Key);
            if (position is not null && position.X.HasValue && position.Y.HasValue)
            {
                var color = System.Drawing.Color.FromArgb(
                    Convert.ToInt32(driver.Value.TeamColour, 16)
                );
                try
                {

                canvas.SetPixel(
                    (position.X.Value + maxCoordVal) / scaleFactor,
                    (position.Y.Value + maxCoordVal) / scaleFactor,
                    new Color(color.R, color.G, color.B)
                );
                } catch {
                    logger.LogError($"Failed to write {position.X.Value} {position.Y.Value} {scaleFactor}");
                }
            }
        }
        canvas.Scale = true;
        return canvas;
    }
}
