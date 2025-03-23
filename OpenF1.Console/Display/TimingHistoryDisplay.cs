using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Microsoft.Extensions.Options;
using OpenF1.Data;
using SkiaSharp;
using Spectre.Console;
using Spectre.Console.Advanced;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingHistoryDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    LapCountProcessor lapCountProcessor,
    TerminalInfoProvider terminalInfo,
    IOptions<LiveTimingOptions> options
) : IDisplay
{
    public Screen Screen => Screen.TimingHistory;

    private const int LEFT_OFFSET = 70; // The normal width of the timing table
    private const int BOTTOM_OFFSET = 2;
    private const int LAPS_IN_CHART = 15;

    private readonly Style _personalBest = new(
        foreground: Color.White,
        background: new Color(0, 118, 0)
    );
    private readonly Style _overallBest = new(
        foreground: Color.White,
        background: new Color(118, 0, 118)
    );
    private readonly Style _normal = new(foreground: Color.White);
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

    private string _chartPanelControlSequence = string.Empty;

    public Task<IRenderable> GetContentAsync()
    {
        var timingTower = GetTimingTower();
        _chartPanelControlSequence = GetChartPanel();

        var layout = new Layout("Root").SplitRows(new Layout("Timing Tower", timingTower));

        return Task.FromResult<IRenderable>(layout);
    }

    /// <inheritdoc />
    public async Task PostContentDrawAsync()
    {
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(0, LEFT_OFFSET));
        await Terminal.OutAsync(_chartPanelControlSequence);
    }

    private IRenderable GetTimingTower()
    {
        var selectedLapNumber = state.CursorOffset + 1;
        var selectedLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber);
        var previousLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber - 1);

        if (selectedLapDrivers is null)
            return new Text($"No Data for Lap {selectedLapNumber}");

        var table = new Table();
        table
            .AddColumns(
                $"LAP {selectedLapNumber, 2}/{lapCountProcessor.Latest?.TotalLaps}",
                "Gap",
                "Interval",
                "Last Lap",
                "S1",
                "S2",
                "S3",
                " "
            )
            .NoBorder();

        foreach (var (driverNumber, line) in selectedLapDrivers.OrderBy(x => x.Value.Line))
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var previousLap = previousLapDrivers?.GetValueOrDefault(driverNumber) ?? new();
            var teamColour = driver.TeamColour ?? "000000";

            var positionChange = line.Line - previousLap.Line;

            var driverTagDecoration = state.SelectedDrivers.Contains(driverNumber)
                ? Decoration.None
                : Decoration.Dim;

            table.AddRow(
                DisplayUtils.DriverTag(driver, line, decoration: driverTagDecoration),
                new Markup(
                    $"{line.GapToLeader}{GetMarkedUp(line.GapToLeaderSeconds() - previousLap.GapToLeaderSeconds())}"
                        ?? "",
                    _normal
                ),
                new Markup(
                    $"{line.IntervalToPositionAhead?.Value}{GetMarkedUp(line.IntervalToPositionAhead?.IntervalSeconds() - previousLap.IntervalToPositionAhead?.IntervalSeconds())}"
                        ?? "",
                    _normal
                ),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                ),
                new Markup(GetPositionChangeMarkup(positionChange))
            );
        }

        return table;
    }

    private string GetMarkedUp(decimal? time) =>
        time switch
        {
            < 0 => $"[green dim italic]{time}[/]",
            < 0.5m => $"[grey62 dim italic]+{time}[/]",
            null => "",
            _ => $"[yellow dim italic]+{time}[/]",
        };

    private string GetPositionChangeMarkup(int? change) =>
        change switch
        {
            < 0 => "[green]▲[/]",
            > 0 => "[yellow]▼[/]",
            _ => "",
        };

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return _normal;
        if (time.OverallFastest ?? false)
            return _overallBest;
        if (time.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }

    private string GetChartPanel()
    {
        if (
            !terminalInfo.IsITerm2ProtocolSupported.Value
            && !terminalInfo.IsKittyProtocolSupported.Value
        )
        {
            return string.Empty;
        }

        var widthCells = Terminal.Size.Width - LEFT_OFFSET;
        var heightCells = Terminal.Size.Height - BOTTOM_OFFSET;
        // Double the width as cells are twice as high as wide
        var ratio = widthCells * 2 / heightCells;

        var heightPixels = 1000;
        var widthPixels = heightPixels * ratio;

        var surface = SKSurface.Create(new SKImageInfo(widthPixels, heightPixels));
        var canvas = surface.Canvas;

        var seriesData = driverList
            .Latest.Where(x => x.Key != "_kf") // Data quirk, dictionaries include _kf which obviously isn't a driver
            .ToDictionary(x => x.Key, _ => new List<double>());

        // Only use data from the last LAPS_IN_CHART laps
        foreach (
            var (lap, lines) in timingData
                .DriversByLap.Skip(state.CursorOffset - LAPS_IN_CHART + 1)
                .Take(LAPS_IN_CHART)
        )
        {
            foreach (var (driver, timingData) in lines)
            {
                seriesData[driver].Add((double)(timingData.GapToLeaderSeconds() ?? 0));
            }
        }

        var series = seriesData
            .Select(x =>
            {
                var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                var colour = driver.TeamColour ?? "FFFFFF";
                return new LineSeries<double>(x.Value)
                {
                    Name = x.Key,
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    GeometryStroke = null,
                    GeometryFill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        StrokeThickness = 2,
                    },
                    IsVisible = state.SelectedDrivers.Contains(x.Key),
                    LineSmoothness = 0,
                    DataLabelsFormatter = p =>
                        p.Index == x.Value.Count - 1 ? driver.Tla! : string.Empty,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                    DataLabelsSize = 16,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse(driver.TeamColour)),
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(1, 0),
                };
            })
            .ToArray();

        var axisStartLap = state.CursorOffset - LAPS_IN_CHART + 1;
        var chart = new LiveChartsCore.SkiaSharpView.SKCharts.SKCartesianChart
        {
            Series = series,
            Height = heightPixels / 2,
            Width = widthPixels,
            Background = SKColors.Black,
            Title = new LabelVisual
            {
                Text = "Gap To Leader (s)",
                Paint = new SolidColorPaint(SKColors.White),
                TextSize = 36,
            },
            XAxes =
            [
                new Axis
                {
                    MinStep = 1,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    Labeler = v =>
                        axisStartLap > 0 ? (v + axisStartLap + 1).ToString() : (v + 1).ToString(),
                },
            ],
            YAxes =
            [
                new Axis
                {
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    MinLimit = 0,
                },
            ],
        };
        chart.DrawOnCanvas(canvas);

        if (options.Value.Verbose)
        {
            // Add some debug information when verbose mode is on
            canvas.DrawRect(0, 0, widthPixels - 1, heightPixels - 1, _errorPaint);
            canvas.DrawText($"Series Count: {series.Count()}", 5, 20, _errorPaint);
            canvas.DrawText(
                $"First Series Data: {string.Join(',', series.ElementAt(1).Values!)}",
                5,
                40,
                _errorPaint
            );
        }

        var imageData = surface.Snapshot().Encode();
        var base64 = Convert.ToBase64String(imageData.AsSpan());

        if (terminalInfo.IsITerm2ProtocolSupported.Value)
        {
            return TerminalGraphics.ITerm2GraphicsSequence(heightCells, widthCells, base64);
        }
        else if (terminalInfo.IsKittyProtocolSupported.Value)
        {
            return TerminalGraphics.KittyGraphicsSequenceDelete()
                + TerminalGraphics.KittyGraphicsSequence(heightCells, widthCells, base64);
        }

        return "Unexpected error, shouldn't have got here. Please report!";
    }
}
