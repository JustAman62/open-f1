using OpenF1.Data;
using SkiaSharp;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class DriverTrackerDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    PositionDataProcessor positionData,
    SessionInfoProcessor sessionInfo,
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
        var circuitPoints = sessionInfo.Latest.CircuitPoints;
        if (circuitPoints.Count == 0)
        {
            return new Text("Circuit info not available");
        }
        
        circuitPoints = circuitPoints.Select(x => (x.x / 100, x.y / 100)).ToList();

        var minX = Math.Abs(circuitPoints.Min(x => x.x));
        var minY = Math.Abs(circuitPoints.Min(x => x.y));

        // Offset the coords to make them all > 0
        circuitPoints = circuitPoints.Select(x => (x.x + minX, x.y + minY)).ToList();

        var maxX = circuitPoints.Max(x => x.x) + 1;
        var maxY = circuitPoints.Max(x => x.y) + 1;
        var max = Math.Max(maxX, maxY);

        // Invert the Y coords due to how Y is displayed in a TUI (top=0 instead bottom=0)
        circuitPoints = circuitPoints.Select(x => (x.x, maxY - x.y)).ToList();

        var windowHeight = System.Console.WindowHeight - 2;
        var windowWidth = windowHeight;

        var img = new SKBitmap(max, max);
        foreach (var (x, y) in circuitPoints)
        {
            try
            {
                img.SetPixel(x, y, SKColor.Parse("111111"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set pixel {}, {}", x, y);
            }
        }

        foreach (var (driverNumber, data) in driverList.Latest)
        {
            var position = positionData
                .Latest.Position.LastOrDefault()
                ?.Entries.GetValueOrDefault(driverNumber);
            if (position is not null && position.X.HasValue && position.Y.HasValue)
            {
                try
                {
                    if (state.SelectedDrivers.Contains(driverNumber))
                    {
                        img.SetPixel(
                            (position.X.Value / 100) + minX,
                            maxY - ((position.Y.Value / 100) + minY),
                            SKColor.Parse(data.TeamColour)
                        );
                    }
                }
                catch
                {
                    logger.LogError(
                        "Failed to draw driver position at {X}, {Y}",
                        position.X.Value,
                        position.Y.Value
                    );
                    img.SetPixel(0, 0, SKColor.Parse("FF0000"));
                }
            }
        }

        var destBitmap = new SKBitmap(windowWidth, windowHeight);
        img.ScalePixels(destBitmap, SKFilterQuality.Medium);
        var pixMap = destBitmap.PeekPixels();

        var canvas = new Canvas(windowWidth, windowHeight);

        foreach (var x in Enumerable.Range(0, windowWidth))
        {
            foreach (var y in Enumerable.Range(0, windowHeight))
            {
                var color = pixMap.GetPixelColor(x, y);
                canvas.SetPixel(x, y, new Color(color.Red, color.Green, color.Blue));
            }
        }

        canvas.Scale = true;
        return canvas;
    }
}
