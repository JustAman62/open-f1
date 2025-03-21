using Microsoft.Extensions.Options;
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
    CarDataProcessor carData,
    SessionInfoProcessor sessionInfo,
    TrackStatusProcessor trackStatus,
    ExtrapolatedClockProcessor extrapolatedClock,
    IDateTimeProvider dateTimeProvider,
    TerminalInfoProvider terminalInfo,
    IOptions<LiveTimingOptions> options,
    ILogger<DriverTrackerDisplay> logger
) : IDisplay
{
    private const int IMAGE_PADDING = 25;
    private const int LEFT_OFFSET = 17;
    private const int TOP_OFFSET = 0;
    private const int BOTTOM_OFFSET = 1;

    private static readonly SKPaint _trackLinePaint = new()
    {
        Color = SKColor.Parse("666666"),
        StrokeWidth = 4,
    };
    private static readonly SKPaint _cornerTextPaint = new()
    {
        Color = SKColor.Parse("DDDDDD"),
        TextSize = 14,
        Typeface = SKTypeface.FromFamilyName(
            "Consolas",
            weight: SKFontStyleWeight.SemiBold,
            width: SKFontStyleWidth.Normal,
            slant: SKFontStyleSlant.Upright
        ),
    };
    private static readonly SKPaint _errorPaint = new()
    {
        Color = SKColor.Parse("FF0000"),
        IsStroke = true,
        Typeface = _boldTypeface,
    };
    private static readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName(
        "Consolas",
        weight: SKFontStyleWeight.ExtraBold,
        width: SKFontStyleWidth.Normal,
        slant: SKFontStyleSlant.Upright
    );

    private string _trackMapControlSequence = string.Empty;

    public Screen Screen => Screen.DriverTracker;

    public Task<IRenderable> GetContentAsync()
    {
        var trackMapMessage = string.Empty;
        if (
            !terminalInfo.IsITerm2ProtocolSupported.Value
            && !terminalInfo.IsKittyProtocolSupported.Value
        )
        {
            // We don't think the current terminal supports the iTerm2 graphics protocol
            trackMapMessage = $"""
                It seems the current terminal may not support inline graphics, which means we can't show the driver tracker.
                If you think this is incorrect, please open an issue at https://github.com/JustAman62/open-f1. 
                Include the diagnostic information below:

                LC_TERMINAL: {Environment.GetEnvironmentVariable("LC_TERMINAL")}
                TERM: {Environment.GetEnvironmentVariable("TERM")}
                TERM_PROGRAM: {Environment.GetEnvironmentVariable("TERM_PROGRAM")}
                """;
        }
        var driverTower = GetDriverTower();
        var statusPanel = GetStatusPanel();
        var layout = new Layout("Content").SplitColumns(
            new Layout("Left Tower")
                .SplitRows(
                    new Layout("Driver List", driverTower),
                    new Layout("Status", statusPanel).Size(6)
                )
                .Size(LEFT_OFFSET - 1),
            new Layout("Track Map", new Text(trackMapMessage)) // Drawn over manually in PostContentDrawAsync()
        );

        _trackMapControlSequence = GetTrackMap();

        return Task.FromResult<IRenderable>(layout);
    }

    private Table GetDriverTower()
    {
        var table = new Table();
        table
            .AddColumns(
                new TableColumn("Drivers") { Width = 8 },
                new TableColumn("Gap") { Width = 7, Alignment = Justify.Right }
            )
            .NoBorder()
            .NoSafeBorder()
            .RemoveColumnPadding();

        var comparisonDataPoint = timingData.Latest.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );

        foreach (var (driverNumber, line) in timingData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var car = carData.Latest.Entries.FirstOrDefault()?.Cars.GetValueOrDefault(driverNumber);
            var isComparisonLine = line == comparisonDataPoint.Value;

            var driverTag = DisplayUtils.MarkedUpDriverNumber(driver);
            var decoration = Decoration.None;
            if (!state.SelectedDrivers.Contains(driverNumber))
            {
                driverTag = $"[dim]{driverTag}[/]";
                decoration |= Decoration.Dim;
            }

            driverTag = state.CursorOffset == line.Line ? $">{driverTag}<" : $" {driverTag} ";

            if (sessionInfo.Latest.IsRace())
            {
                table.AddRow(
                    new Markup(driverTag),
                    state.CursorOffset > 0
                        ? DisplayUtils.GetGapBetweenLines(
                            comparisonDataPoint.Value,
                            line,
                            decoration
                        )
                        : new Text(
                            $"{(car?.Channels.Drs >= 8 ? "•" : "")}{line.IntervalToPositionAhead?.Value}".ToFixedWidth(
                                7
                            ),
                            DisplayUtils.GetStyle(
                                line.IntervalToPositionAhead,
                                false,
                                car,
                                decoration
                            )
                        )
                );
            }
            else
            {
                var bestDriver = timingData.Latest.GetOrderedLines().First();
                var position =
                    positionData
                        .Latest.Position.LastOrDefault()
                        ?.Entries.GetValueOrDefault(driverNumber) ?? new();
                var gapToLeader = (
                    line.BestLapTime.ToTimeSpan() - bestDriver.Value.BestLapTime.ToTimeSpan()
                )?.TotalSeconds;

                table.AddRow(
                    new Markup(driverTag),
                    position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack
                        ? new Text(
                            "OFF TRK",
                            new Style(background: Color.Red, foreground: Color.White)
                        )
                        : new Text(
                            $"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".ToFixedWidth(7),
                            DisplayUtils.STYLE_NORMAL.Combine(new Style(decoration: decoration))
                        )
                );
            }
        }

        return table;
    }

    private Panel GetStatusPanel()
    {
        var items = new List<IRenderable>();

        if (trackStatus.Latest is not null)
        {
            var style = trackStatus.Latest.Status switch
            {
                "1" => DisplayUtils.STYLE_PB, // All Clear
                "2" => new Style(foreground: Color.Black, background: Color.Yellow), // Yellow Flag
                "4" => new Style(foreground: Color.Black, background: Color.Yellow), // Safety Car
                "6" => new Style(foreground: Color.Black, background: Color.Yellow), // VSC Deployed
                "5" => new Style(foreground: Color.White, background: Color.Red), // Red Flag
                _ => Style.Plain,
            };
            items.Add(new Text($"{trackStatus.Latest.Message}", style));
        }

        items.Add(new Text($@"{dateTimeProvider.Utc:HH\:mm\:ss}"));
        items.Add(new Text($@"{extrapolatedClock.ExtrapolatedRemaining():hh\:mm\:ss}"));

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Status"),
            Expand = true,
            Border = BoxBorder.Rounded,
        };
    }

    private string GetTrackMap()
    {
        if (
            !(
                terminalInfo.IsITerm2ProtocolSupported.Value
                || terminalInfo.IsKittyProtocolSupported.Value
            )
            || sessionInfo.Latest.CircuitPoints.Count == 0
        )
        {
            return string.Empty;
        }

        var imageScaleFactor = sessionInfo.Latest.CircuitPoints.Max(x => x.x) / 350;

        var circuitPoints = sessionInfo
            .Latest.CircuitPoints.Select(x =>
                (x: x.x / imageScaleFactor, y: x.y / imageScaleFactor)
            )
            .ToList();

        var minX = Math.Abs(circuitPoints.Min(x => x.x)) + IMAGE_PADDING;
        var minY = Math.Abs(circuitPoints.Min(x => x.y)) + IMAGE_PADDING;

        // Offset the coords to make them all > 0
        circuitPoints = circuitPoints.Select(x => (x.x + minX, x.y + minY)).ToList();

        var maxX = circuitPoints.Max(x => x.x) + IMAGE_PADDING;
        var maxY = circuitPoints.Max(x => x.y) + IMAGE_PADDING;

        // Invert the Y coords due to how Y is displayed in a TUI (top=0 instead bottom=0)
        circuitPoints = circuitPoints.Select(x => (x.x, maxY - x.y)).ToList();

        // Draw the image as a square that fits the actual track map in
        var longestEdgeLength = Math.Max(maxX, maxY);
        var surface = SKSurface.Create(new SKImageInfo(longestEdgeLength, longestEdgeLength));
        var canvas = surface.Canvas;

        // Draw lines between all the points of the track to create the track map
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
                canvas.DrawCircle(5, 5, 2, _errorPaint);
            }
        }

        var circuitCorners = sessionInfo
            .Latest.CircuitCorners.Select(x =>
                (x.number, x: x.x / imageScaleFactor, y: x.y / imageScaleFactor)
            )
            .Select(x => (x.number, x: x.x + minX, y: maxY - (x.y + minY)))
            .ToList();

        foreach (var (number, x, y) in circuitCorners)
        {
            try
            {
                // Draw the text to the right of the corner
                canvas.DrawText(number.ToString(), x + 10, y, _cornerTextPaint);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to add corner number {corner} at {x},{y}",
                    number,
                    x,
                    y
                );
                canvas.DrawCircle(5, 5, 2, _errorPaint);
            }
        }

        // Add all the selected drivers positions to the map
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
                        var x = (position.X.Value / imageScaleFactor) + minX;
                        var y = maxY - ((position.Y.Value / imageScaleFactor) + minY);
                        var paint = new SKPaint
                        {
                            Color = SKColor.Parse(data.TeamColour),
                            TextSize = 14,
                            Typeface = _boldTypeface,
                        };

                        // Draw a white box around the driver currently selected by the cursor
                        if (timingData.Latest.Lines[driverNumber].Line == state.CursorOffset)
                        {
                            var rectPaint = new SKPaint { Color = SKColor.Parse("FFFFFF") };
                            canvas.DrawRoundRect(x - 6, y - 8, 46, 16, 4, 4, rectPaint);
                        }

                        canvas.DrawCircle(x, y, 5, paint);
                        canvas.DrawText(
                            data.Tla,
                            (position.X.Value / imageScaleFactor) + minX + 8,
                            maxY - ((position.Y.Value / imageScaleFactor) + minY) + 6,
                            paint
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
                    canvas.DrawCircle(5, 10, 2, _errorPaint);
                }
            }
        }

        var windowHeight = Terminal.Size.Height - TOP_OFFSET - BOTTOM_OFFSET;
        var windowWidth = Terminal.Size.Width - LEFT_OFFSET;
        // Terminal protocols will distort the image, so provide height/width as the biggest square that will definitely fit
        // Terminal cells are twice as high as they are wide, so take that in to consideration
        var shortestWindowEdgeLength = Math.Min(windowWidth, windowHeight * 2);
        windowHeight = shortestWindowEdgeLength / 2;
        windowWidth = shortestWindowEdgeLength;

        if (options.Value.Verbose)
        {
            // Add some debug information when verbose mode is on
            canvas.DrawRect(0, 0, longestEdgeLength - 1, longestEdgeLength - 1, _errorPaint);
            canvas.DrawText(
                $"iTerm2 Support: {terminalInfo.IsITerm2ProtocolSupported.Value}",
                5,
                20,
                _errorPaint
            );
            canvas.DrawText(
                $"Kitty Support: {terminalInfo.IsKittyProtocolSupported.Value}",
                5,
                40,
                _errorPaint
            );
            canvas.DrawText(
                $"Window H/W: {windowHeight}/{windowWidth} Shortest: {shortestWindowEdgeLength}",
                5,
                60,
                _errorPaint
            );
            canvas.DrawText(
                $"Synchronized Output Support: {terminalInfo.IsSynchronizedOutputSupported}",
                5,
                80,
                _errorPaint
            );
            canvas.DrawText($"Image Scale factor: {imageScaleFactor}", 5, 100, _errorPaint);
        }

        var imageData = surface.Snapshot().Encode();
        var base64 = Convert.ToBase64String(imageData.AsSpan());

        if (terminalInfo.IsITerm2ProtocolSupported.Value)
        {
            return TerminalGraphics.ITerm2GraphicsSequence(windowHeight, windowWidth, base64);
        }
        else if (terminalInfo.IsKittyProtocolSupported.Value)
        {
            return TerminalGraphics.KittyGraphicsSequenceDelete()
                + TerminalGraphics.KittyGraphicsSequence(windowHeight, windowWidth, base64);
        }

        return "Unexpected error, shouldn't have got here. Please report!";
    }

    /// <inheritdoc />
    public async Task PostContentDrawAsync()
    {
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(TOP_OFFSET, LEFT_OFFSET));
        await Terminal.OutAsync(_trackMapControlSequence);
    }
}
