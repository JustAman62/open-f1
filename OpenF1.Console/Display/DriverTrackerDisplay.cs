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
    private const int IMAGE_SCALE_FACTOR = 20;
    private const int IMAGE_PADDING = 25;
    private static readonly SKPaint _trackLinePaint =
        new() { Color = SKColor.Parse("666666"), StrokeWidth = 4 };
    private static readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName(
        "Consolas",
        weight: SKFontStyleWeight.Bold,
        width: SKFontStyleWidth.Normal,
        slant: SKFontStyleSlant.Upright
    );

    private string _previousImage = string.Empty;

    public Screen Screen => Screen.DriverTracker;

    public Task<IRenderable> GetContentAsync()
    {
        var driverTower = GetDriverTower();
        var layout = new Layout("Content").SplitColumns(
            new Layout("Driver List", driverTower),
            new Layout("Track Map", new Text(string.Empty)) // Empty, drawn in to manually in PostContentDrawAsync()
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

        circuitPoints = circuitPoints
            .Select(x => (x.x / IMAGE_SCALE_FACTOR, x.y / IMAGE_SCALE_FACTOR))
            .ToList();

        var minX = Math.Abs(circuitPoints.Min(x => x.x)) + IMAGE_PADDING;
        var minY = Math.Abs(circuitPoints.Min(x => x.y)) + IMAGE_PADDING;

        // Offset the coords to make them all > 0
        circuitPoints = circuitPoints.Select(x => (x.x + minX, x.y + minY)).ToList();

        var maxX = circuitPoints.Max(x => x.x) + IMAGE_PADDING;
        var maxY = circuitPoints.Max(x => x.y) + IMAGE_PADDING;
        var max = Math.Max(maxX, maxY);

        // Invert the Y coords due to how Y is displayed in a TUI (top=0 instead bottom=0)
        circuitPoints = circuitPoints.Select(x => (x.x, maxY - x.y)).ToList();

        var surface = SKSurface.Create(new SKImageInfo(maxX, maxY));
        var canvas = surface.Canvas;
        for (var i = 0; i < circuitPoints.Count - 1; i++)
        {
            var a = circuitPoints[i];
            var b = circuitPoints[i + 1];
            try
            {
                canvas.DrawLine(a.x, a.y, b.x, b.y, _trackLinePaint);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set create line from {a} to {b}", a, b);
                canvas.DrawCircle(5, 5, 2, new SKPaint { Color = SKColor.Parse("FF0000") });
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
                        canvas.DrawCircle(
                            (position.X.Value / IMAGE_SCALE_FACTOR) + minX,
                            maxY - ((position.Y.Value / IMAGE_SCALE_FACTOR) + minY),
                            5,
                            new SKPaint { Color = SKColor.Parse(data.TeamColour) }
                        );
                        canvas.DrawText(
                            data.Tla,
                            (position.X.Value / IMAGE_SCALE_FACTOR) + minX + 8,
                            maxY - ((position.Y.Value / IMAGE_SCALE_FACTOR) + minY) + 6,
                            new SKFont { Embolden = true },
                            new SKPaint
                            {
                                Color = SKColor.Parse(data.TeamColour),
                                TextSize = 14,
                                Typeface = _boldTypeface
                            }
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
                    canvas.DrawCircle(5, 10, 2, new SKPaint { Color = SKColor.Parse("FF0000") });
                }
            }
        }

        var imageData = surface.Snapshot().Encode();
        var base64 = Convert.ToBase64String(imageData.AsSpan());

        var output = TerminalGraphics.ITerm2GraphicsSequence(windowHeight, windowWidth, base64);
        // Only draw the image if it has changed
        if (_previousImage != output)
        {
            await Terminal.OutAsync(output);
        }
        _previousImage = output;
    }
}
