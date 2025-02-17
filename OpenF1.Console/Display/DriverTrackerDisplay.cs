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

    public async Task<IRenderable> GetContentAsync()
    {
        var driverTower = GetDriverTower();
        var layout = new Layout("Content").SplitColumns(
            new Layout("Driver List", driverTower),
            new Layout("Track Map", new Text(string.Empty)) // Empty, drawn in to manually in PostContentDrawAsync()
        );
        layout["Content"]["Driver List"].Size = 13;

        return layout;
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

    public async Task PostContentDrawAsync()
    {
        const int LEFT_OFFSET = 14;
        const int TOP_OFFSET = 1;
        const int BOTTOM_OFFSET = 2;
        var windowHeight = Terminal.Size.Height - TOP_OFFSET - BOTTOM_OFFSET;
        var windowWidth = Terminal.Size.Width - LEFT_OFFSET;

        await Terminal.OutAsync(ControlSequences.MoveCursorTo(TOP_OFFSET, LEFT_OFFSET));

        if (!TerminalGraphics.IsITerm2ProtocolSupported())
        {
            // We don't think the current terminal supports the iTerm2 graphics protocol
            await Terminal.OutAsync(
                $"""
                It seems the current terminal may not support inline graphics, which means we can't this the driver tracker.
                If you think this is incorrect, please open an issue at https://github.com/JustAman62/open-f1. Include the diagnostic information below:

                LC_TERMINAL: {Environment.GetEnvironmentVariable("LC_TERMINAL")}
                TERM: {Environment.GetEnvironmentVariable("TERM")}
                TERM_PROGRAM: {Environment.GetEnvironmentVariable("TERM_PROGRAM")}
                """
            );
            return;
        }

        var circuitPoints = sessionInfo.Latest.CircuitPoints;
        if (circuitPoints.Count == 0)
        {
            await Terminal.OutAsync("Circuit info not available");
            return;
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

        var img = new SKBitmap(maxX, maxY);
        foreach (var (x, y) in circuitPoints)
        {
            try
            {
                img.SetPixel(x, y, SKColor.Parse("666666"));
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

        var imageData = img.Encode(SKEncodedImageFormat.Png, 100);
        var base64 = Convert.ToBase64String(imageData.AsSpan());

        var output = TerminalGraphics.ITerm2GraphicsSequence(windowHeight, windowWidth, base64);
        await Terminal.OutAsync(output);
    }
}
