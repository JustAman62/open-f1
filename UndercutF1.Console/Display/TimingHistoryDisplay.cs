using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;

namespace UndercutF1.Console;

public class TimingHistoryDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    LapCountProcessor lapCount,
    SessionInfoProcessor sessionInfo,
    TerminalInfoProvider terminalInfo,
    IOptions<ConsoleOptions> options
) : IDisplay
{
    public Screen Screen => Screen.TimingHistory;

    private const int LEFT_OFFSET = 69; // The normal width of the timing table
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

    private static readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName(
        "Consolas",
        weight: SKFontStyleWeight.ExtraBold,
        width: SKFontStyleWidth.Normal,
        slant: SKFontStyleSlant.Upright
    );
    private static readonly SKFont _boldFont = new(_boldTypeface);
    private static readonly SKPaint _errorPaint = new()
    {
        Color = SKColor.Parse("FF0000"),
        IsStroke = true,
        IsAntialias = false,
    };

    private static readonly SolidColorPaint _lightGrayPaint = new(SKColors.DimGray);
    private static readonly SolidColorPaint _labelsPaint = new(SKColors.DimGray);

    private static readonly SolidColorPaint _whitePaint = new(SKColors.White)
    {
        IsAntialias = false,
    };

    private string[] _chartPanelControlSequence = [];
    private string[] _previousSequence = [];

    public Task<IRenderable> GetContentAsync()
    {
        var timingTower = GetTimingTower();

        _chartPanelControlSequence = GetChartPanel();

        var layout = new Layout("Root").SplitRows(new Layout("Timing Tower", timingTower));

        return Task.FromResult<IRenderable>(layout);
    }

    /// <inheritdoc />
    public async Task PostContentDrawAsync(bool shouldDraw)
    {
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(0, LEFT_OFFSET));

        // Only draw if we need to, or if the drawing has changed
        var hasChanged = !_previousSequence.SequenceEqual(_chartPanelControlSequence);
        if (shouldDraw || hasChanged)
        {
            foreach (var sequence in _chartPanelControlSequence)
            {
                await Terminal.OutAsync(sequence);
                _previousSequence = _chartPanelControlSequence;
            }
        }
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
                $"LAP {selectedLapNumber, 2}/{lapCount.Latest?.TotalLaps}",
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

            var driverTagDecoration = driver.IsSelected ? Decoration.None : Decoration.Dim;

            table.AddRow(
                DisplayUtils.DriverTag(
                    driver,
                    line,
                    positionChange: line.Line - previousLap.Line,
                    decoration: driverTagDecoration
                ),
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
                )
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

    private string[] GetChartPanel()
    {
        if (
            !terminalInfo.IsITerm2ProtocolSupported.Value
            && !terminalInfo.IsKittyProtocolSupported.Value
            && !terminalInfo.IsSixelSupported.Value
        )
        {
            return [];
        }

        var isRace = sessionInfo.Latest.IsRace();

        var widthCells = Terminal.Size.Width - LEFT_OFFSET;
        var heightCells = Terminal.Size.Height - BOTTOM_OFFSET;

        var terminalHeightPixels = terminalInfo.TerminalSize.Value.Height;
        var heightPerCell = terminalHeightPixels / Terminal.Size.Height;

        var terminalWidthPixels = terminalInfo.TerminalSize.Value.Width;
        var widthPerCell = terminalWidthPixels / Terminal.Size.Width;

        var heightPixels = heightCells * heightPerCell;
        var widthPixels = widthCells * widthPerCell;

        var surface = SKSurface.Create(
            new SKImageInfo(widthPixels, heightPixels, SKColorType.Argb4444)
        );
        var canvas = surface.Canvas;

        var gapSeriesData = driverList
            .Latest.Where(x => x.Key != "_kf") // Data quirk, dictionaries include _kf which obviously isn't a driver
            .ToDictionary(x => x.Key, _ => new List<ObservablePoint>());
        var lapSeriesData = driverList
            .Latest.Where(x => x.Key != "_kf") // Data quirk, dictionaries include _kf which obviously isn't a driver
            .ToDictionary(x => x.Key, _ => new List<ObservablePoint>());

        var fastestLap = default(TimeSpan);

        // Only use data from the last LAPS_IN_CHART laps
        // Lap numbers are 1-indexed, cursor is 0-indexed, so offset by 1
        var minLap = state.CursorOffset - LAPS_IN_CHART + 1;
        var maxLap = state.CursorOffset + 1;
        foreach (
            var (lap, lines) in timingData
                .DriversByLap.OrderBy(x => x.Key)
                .Where(x => x.Key >= minLap && x.Key <= maxLap)
        )
        {
            fastestLap =
                lines.Min(x => x.Value.LastLapTime?.ToTimeSpan()) ?? TimeSpan.FromMinutes(2);

            // Use an arbitrary threshold of the current laps fatest lap + 30sec to discard slow laps
            // which would distort the chart. 30secs should be enought o show race and quali pace on the same chart.
            var threshold = fastestLap + TimeSpan.FromSeconds(30);
            foreach (var (driver, timingData) in lines)
            {
                // Lapped cars don't have a gap to leader, so use the smart calc to determine the real gap
                var gapToLeader = lines.SmartGapToLeaderSeconds(driver);
                if (gapToLeader.HasValue)
                {
                    var value = new ObservablePoint(lap, Convert.ToDouble(gapToLeader.Value));
                    gapSeriesData[driver].Add(value);
                }
                else
                {
                    gapSeriesData[driver].Add(new(lap, null));
                }

                var lapTime = timingData.LastLapTime?.ToTimeSpan();
                // Use the threshold to null out laps that are too slow
                // (attempting to avoid in and out laps from skewing the chart)
                if (lapTime > threshold || timingData.IsPitLap)
                {
                    lapSeriesData[driver].Add(new(lap, null));
                }
                else
                {
                    var value = new ObservablePoint(lap, lapTime?.TotalMilliseconds);
                    lapSeriesData[driver].Add(value);
                }
            }
        }

        if (isRace)
        {
            var gapSeries = gapSeriesData
                .Select(x =>
                {
                    var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                    var colour = driver.TeamColour ?? "FFFFFF";
                    return new LineSeries<ObservablePoint?>(x.Value)
                    {
                        Name = x.Key,
                        Fill = new SolidColorPaint(SKColors.Transparent),
                        GeometryStroke = null,
                        GeometryFill = null,
                        Stroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                        {
                            StrokeThickness = 2,
                        },
                        IsVisible = driverList.IsSelected(x.Key),
                        LineSmoothness = 0,
                        // Add the drivers name next to the final data point, as a series label
                        DataLabelsFormatter = p =>
                            p.Index == x.Value.Count - 1 ? driver.Tla! : string.Empty,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                        DataLabelsSize = 16,
                        DataLabelsPaint = new SolidColorPaint(SKColor.Parse(driver.TeamColour)),
                        DataPadding = new LiveChartsCore.Drawing.LvcPoint(1, 0),
                    };
                })
                .ToArray();

            var gapChart = CreateChart(
                gapSeries,
                "Gap to Leader (s)",
                heightPixels / 2,
                widthPixels,
                labeler: Labelers.Default,
                axisMin: 0,
                yMinStep: 1
            );
            gapChart.DrawOnCanvas(canvas);
        }

        var lapSeries = lapSeriesData
            .Select(x =>
            {
                var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                var colour = driver.TeamColour ?? "FFFFFF";
                return new LineSeries<ObservablePoint?>(x.Value)
                {
                    Name = x.Key,
                    Fill = new SolidColorPaint(SKColors.Transparent) { IsAntialias = false },
                    GeometrySize = 4,
                    GeometryStroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        IsAntialias = false,
                        StrokeThickness = 2,
                    },
                    GeometryFill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        IsAntialias = false,
                        StrokeThickness = 2,
                    },
                    IsVisible = driverList.IsSelected(x.Key),
                    LineSmoothness = 0,
                    // Add the drivers name next to the final data point, as a series label
                    DataLabelsFormatter = p =>
                        p.Index == x.Value.Count - 1 || x.Value[p.Index + 1].Y == null
                            ? driver.Tla!
                            : string.Empty,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                    DataLabelsSize = 16,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        IsAntialias = false,
                    },
                    DataPadding = new LvcPoint(1, 0),
                };
            })
            .ToArray();

        var lapChart = CreateChart(
            lapSeries,
            "Lap Time",
            isRace ? heightPixels / 2 : heightPixels,
            widthPixels,
            labeler: v => TimeSpan.FromMilliseconds(v).ToString("mm':'ss"),
            yMinStep: 1000
        );
        var lapChartImage = lapChart.GetImage();
        canvas.DrawImage(lapChartImage, new SKPoint(0, isRace ? heightPixels / 2 : 0));

        if (options.Value.Verbose)
        {
            // Add some debug information when verbose mode is on
            canvas.DrawRect(0, 0, widthPixels - 1, heightPixels - 1, _errorPaint);
            canvas.DrawText($"Width: {widthPixels}", 5, 20, _boldFont, _errorPaint);
            canvas.DrawText($"Height: {heightPixels}", 5, 40, _boldFont, _errorPaint);
            canvas.DrawText($"AlphaType: {lapChartImage.AlphaType}", 5, 60, _boldFont, _errorPaint);
            canvas.DrawText($"ColorType: {lapChartImage.ColorType}", 5, 80, _boldFont, _errorPaint);
            canvas.DrawText(
                $"ColorSpace: {lapChartImage.ColorSpace}",
                5,
                100,
                _boldFont,
                _errorPaint
            );
        }

        return surface.Snapshot().ToGraphicsSequence(terminalInfo, heightCells, widthCells);
    }

    private SKCartesianChart CreateChart(
        LineSeries<ObservablePoint?>[] series,
        string title,
        int height,
        int width,
        Func<double, string> labeler,
        double? axisMin = null,
        double? axisMax = null,
        double yMinStep = 0
    ) =>
        new()
        {
            Series = series,
            Height = height,
            Width = width,
            Background = SKColors.Transparent,
            Title = new DrawnLabelVisual(
                new()
                {
                    Text = title,
                    Paint = _whitePaint,
                    TextSize = 20,
                    HorizontalAlign = LiveChartsCore.Drawing.Align.Start,
                    Padding = new() { Top = 24 },
                }
            ),
            XAxes =
            [
                new Axis
                {
                    MinStep = 1,
                    LabelsPaint = _labelsPaint,
                    Padding = new LiveChartsCore.Drawing.Padding() { Top = 4, Right = 36 },
                },
            ],
            YAxes =
            [
                new Axis
                {
                    SeparatorsPaint = _lightGrayPaint,
                    LabelsPaint = _labelsPaint,
                    MinLimit = axisMin,
                    MaxLimit = axisMax,
                    Labeler = labeler,
                    MinStep = yMinStep,
                    LabelsDensity = 2,
                    Padding = new LiveChartsCore.Drawing.Padding() { Right = 4 },
                },
            ],
            ChartTheme = new() { VirtualBackroundColor = LvcColor.Empty },
        };
}
