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

    private readonly HashSet<(int x, int y)> _previousCoords = new();

    public Task<IRenderable> GetContentAsync()
    {
        var driverTower = GetDriverTower();
        var details = GetDetails();
        var layout = new Layout("Content").SplitColumns(
            new Layout("Driver List", driverTower),
            new Layout("Details", details)
        );
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
        var height = System.Console.WindowHeight - 3;
        var maxCoordVal = 10000;
        var scaleFactor = maxCoordVal / height;
        var canvas = new Canvas(height + 1, height + 1);

        foreach (var (x, y) in _previousCoords)
        {
            canvas.SetPixel(x, y, Color.Grey23);
        }

        foreach (var (driverNumber, data) in driverList.Latest)
        {
            var position = positionData
                .Latest.Position.LastOrDefault()
                ?.Entries.GetValueOrDefault(driverNumber);
            if (position is not null && position.X.HasValue && position.Y.HasValue)
            {
                var color = System.Drawing.Color.FromArgb(Convert.ToInt32(data.TeamColour, 16));
                try
                {
                    var x = (position.X.Value + maxCoordVal) / scaleFactor;
                    // Console coords have 0,0 in top-left, so flip the y scale
                    var y = height - ((position.Y.Value + maxCoordVal) / scaleFactor);
                    _previousCoords.Add((x, y));

                    if (state.SelectedDrivers.Contains(driverNumber))
                    {
                        canvas.SetPixel(x, y, new Color(color.R, color.G, color.B));
                    }
                }
                catch
                {
                    logger.LogError(
                        $"Failed to write {position.X.Value} {position.Y.Value} {scaleFactor}"
                    );
                    canvas.SetPixel(0, 0, Color.Red);
                }
            }
        }
        canvas.Scale = true;
        return canvas;
    }
}
