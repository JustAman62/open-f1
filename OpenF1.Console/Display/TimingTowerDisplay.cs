using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingTowerDisplay(
    State state,
    RaceControlMessageProcessor raceControlMessages,
    TimingDataProcessor timingData,
    TimingAppDataProcessor timingAppData,
    DriverListProcessor driverList,
    TrackStatusProcessor trackStatusProcessor,
    LapCountProcessor lapCountProcessor,
    SessionInfoProcessor sessionInfoProcessor,
    ITimingService timingService
) : IDisplay
{
    public Screen Screen => Screen.TimingTower;

    private const int STATUS_PANEL_WIDTH = 15;

    private readonly Style _personalBest =
        new(foreground: Color.White, background: new Color(0, 118, 0));
    private readonly Style _overallBest =
        new(foreground: Color.White, background: new Color(118, 0, 118));
    private readonly Style _normal = new(foreground: Color.White);
    private readonly Style _invert = new(foreground: Color.Black, background: Color.White);

    public Task<IRenderable> GetContentAsync()
    {
        var statusPanel = GetStatusPanel();

        var raceControlPanel =
            state.CursorOffset > 0 && sessionInfoProcessor.Latest.IsRace()
                ? GetComparisonPanel()
                : GetRaceControlPanel();
        var timingTower = sessionInfoProcessor.Latest.IsRace()
            ? GetRaceTimingTower()
            : GetNonRaceTimingTower();

        var layout = new Layout("Root").SplitRows(
            new Layout("Content").SplitColumns(
                new Layout("Timing Tower", timingTower).Size(System.Console.WindowWidth)
            ),
            new Layout("Info").SplitColumns(
                new Layout("Status", statusPanel),
                new Layout("Race Control Messages", raceControlPanel)
            )
        );

        layout["Info"].Size = 5;
        layout["Info"]["Status"].Size = STATUS_PANEL_WIDTH;

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetRaceTimingTower()
    {
        if (timingData.Latest is null)
            return new Text("No Timing");

        var table = new Table();
        table
            .AddColumns(
                $"LAP {lapCountProcessor.Latest?.CurrentLap, 2}/{lapCountProcessor.Latest?.TotalLaps}",
                "Gap   ",
                "Interval",
                "Best Lap",
                "Last Lap",
                "S1",
                "S2",
                "S3",
                "Pit",
                "Tyre",
                "Compare",
                "Driver",
                " "
            )
            .RemoveColumnPadding();

        // Increase padding between lap and sector times to clearly mark them
        table.Columns[5].Padding = new Padding(left: 1, 0, 0, 0);

        var comparisonDataPoint = timingData.Latest.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );

        var lapNumber = lapCountProcessor.Latest?.CurrentLap ?? 0;

        foreach (var (driverNumber, line) in timingData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var previousLapDrivers = timingData.DriversByLap.GetValueOrDefault(
                line.NumberOfLaps - 1 ?? 0
            );
            var previousLapLine = previousLapDrivers?.GetValueOrDefault(driverNumber) ?? new();
            var teamColour = driver.TeamColour ?? "000000";

            var positionChange = line.Line - previousLapLine.Line;

            var isComparisonLine = line == comparisonDataPoint.Value;
            var lineStyle = isComparisonLine ? _invert : _normal;

            table.AddRow(
                new Markup(
                    $"{line.Position, 2} [#{teamColour} bold]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]",
                    lineStyle
                ),
                new Text($"{line.GapToLeader, 7}", lineStyle),
                new Text(
                    $"{line.IntervalToPositionAhead?.Value, 8}{(line.IntervalToPositionAhead?.Catching ?? false ? "▲" : string.Empty)}",
                    GetStyle(line.IntervalToPositionAhead, isComparisonLine)
                ),
                new Text(line.BestLapTime?.Value ?? "NULL", _normal),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                ),
                new Text(
                    line.InPit.GetValueOrDefault()
                        ? "IN "
                        : line.PitOut.GetValueOrDefault()
                            ? "OUT"
                            : $"{line.NumberOfPitStops, 2}",
                    line.InPit.GetValueOrDefault()
                        ? new Style(foreground: Color.Black, background: Color.Yellow)
                        : line.PitOut.GetValueOrDefault()
                            ? new Style(foreground: Color.Black, background: Color.Green)
                            : Style.Plain
                ),
                new Text($"{stint?.Compound?[0]} {stint?.TotalLaps, 2}", GetStyle(stint)),
                GetGapBetweenLines(comparisonDataPoint.Value, line),
                new Markup(
                    $"[#{teamColour}]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]",
                    lineStyle
                ),
                new Markup(GetPositionChangeMarkup(positionChange))
            );
        }

        table.SimpleBorder();
        table.Expand();

        return table;
    }

    private IRenderable GetNonRaceTimingTower()
    {
        if (timingData.Latest is null)
            return new Text("No Timing Available");

        var table = new Table();
        table
            .AddColumns(
                sessionInfoProcessor.Latest.Name?.ToFixedWidth(9) ?? "Unknown ",
                "Gap    ",
                "Best Lap",
                "BS1",
                "BS2",
                "BS3",
                "S1",
                "S2",
                "S3",
                "Pit",
                "Tyre"
            )
            .RemoveColumnPadding();

        // Increase padding between sets of sectors to clearly mark them
        table.Columns[3].Padding = new Padding(left: 1, 0, 0, 0);
        table.Columns[6].Padding = new Padding(left: 1, 0, 0, 0);

        var bestDriver = timingData.Latest.GetOrderedLines().First();

        var bestSector1 = timingData
            .BestLaps.DefaultIfEmpty()
            .MinBy(x => x.Value?.Sectors.GetValueOrDefault("0")?.ToTimeSpan())
            .Value?.Sectors?["0"];
        var bestSector2 = timingData
            .BestLaps.DefaultIfEmpty()
            .MinBy(x => x.Value?.Sectors.GetValueOrDefault("1")?.ToTimeSpan())
            .Value?.Sectors?["1"];
        var bestSector3 = timingData
            .BestLaps.DefaultIfEmpty()
            .MinBy(x => x.Value?.Sectors.GetValueOrDefault("2")?.ToTimeSpan())
            .Value?.Sectors?["2"];

        foreach (var (driverNumber, line) in timingData.Latest.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var bestLap = timingData.BestLaps.GetValueOrDefault(driverNumber);
            var teamColour = driver.TeamColour ?? "000000";

            var gapToLeader = (
                line.BestLapTime.ToTimeSpan() - bestDriver.Value.BestLapTime.ToTimeSpan()
            )?.TotalSeconds;

            table.AddRow(
                new Markup(
                    $"{line.Position, 2} [#{teamColour} bold]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]",
                    _normal
                ),
                new Text($"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".PadLeft(7), _normal),
                new Text(line.BestLapTime?.Value ?? "NULL", _normal),
                GetSectorMarkup(
                    bestLap?.Sectors.GetValueOrDefault("0"),
                    bestSector1,
                    overrideStyle: true,
                    showDifference: true
                ),
                GetSectorMarkup(
                    bestLap?.Sectors.GetValueOrDefault("1"),
                    bestSector2,
                    overrideStyle: true,
                    showDifference: true
                ),
                GetSectorMarkup(
                    bestLap?.Sectors.GetValueOrDefault("2"),
                    bestSector3,
                    overrideStyle: true,
                    showDifference: true
                ),
                GetSectorMarkup(line.Sectors.GetValueOrDefault("0"), bestSector1),
                GetSectorMarkup(line.Sectors.GetValueOrDefault("1"), bestSector2),
                GetSectorMarkup(line.Sectors.GetValueOrDefault("2"), bestSector3),
                new Text(
                    line.InPit.GetValueOrDefault()
                        ? "IN "
                        : line.PitOut.GetValueOrDefault()
                            ? "OUT"
                            : $"{line.NumberOfPitStops, 2}",
                    line.InPit.GetValueOrDefault()
                        ? new Style(foreground: Color.Black, background: Color.Yellow)
                        : line.PitOut.GetValueOrDefault()
                            ? _personalBest
                            : Style.Plain
                ),
                new Text($"{stint?.Compound?[0] ?? 'X'} {stint?.TotalLaps, 2}", GetStyle(stint))
            );
        }

        table.SimpleBorder();
        table.Expand();

        return table;
    }

    private IRenderable GetSectorMarkup(
        TimingDataPoint.Driver.LapSectorTime? time,
        TimingDataPoint.Driver.LapSectorTime? bestSector = null,
        bool overrideStyle = false,
        bool showDifference = false
    )
    {
        var sector =
            $"[{GetStyle(time, bestSector, overrideStyle).ToMarkup()}]{time?.Value?.PadLeft(6)}[/]";

        var differenceToBest = time?.ToTimeSpan() - bestSector?.ToTimeSpan();
        var differenceColor = differenceToBest < TimeSpan.Zero ? "green" : "white";
        var difference = differenceToBest.HasValue
            ? $"[dim italic {differenceColor}]({(differenceToBest >= TimeSpan.Zero ? "+" : string.Empty)}{differenceToBest:s\\.fff})[/]"
            : string.Empty;
        return differenceToBest < TimeSpan.FromSeconds(10) && showDifference
            ? new Markup($"{sector}{difference}")
            : new Markup($"{sector}");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(
        TimingDataPoint.Driver.LapSectorTime? time,
        TimingDataPoint.Driver.LapSectorTime? bestSector = null,
        bool overrideStyle = false
    )
    {
        if (string.IsNullOrWhiteSpace(time?.Value))
            return _normal;

        if (bestSector?.ToTimeSpan() == time?.ToTimeSpan())
            return _overallBest;

        // If we are checking against a best sector, don't style it unless it is fastest
        if (overrideStyle)
            return _normal;

        if (time?.OverallFastest ?? false)
            return _overallBest;
        if (time?.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingAppDataPoint.Driver.Stint? stint)
    {
        if (stint is null)
            return _normal;

        return stint.Compound switch
        {
            "HARD" => new Style(foreground: Color.White, background: Color.Grey),
            "MEDIUM" => new Style(foreground: Color.Black, background: Color.Yellow),
            "SOFT" => new Style(foreground: Color.White, background: Color.Red),
            "INTER" => new Style(foreground: Color.Black, background: Color.Green),
            "WET" => new Style(foreground: Color.Black, background: Color.Blue),
            _ => _normal
        };
    }

    private Style GetStyle(TimingDataPoint.Driver.Interval? interval, bool isComparisonLine)
    {
        if (interval is null)
            return _normal;

        var foreground = Color.White;
        var background = Color.Black;

        if (isComparisonLine)
        {
            foreground = Color.Black;
            background = Color.White;
        }

        if (interval.IntervalSeconds() < 1 && interval.IntervalSeconds() > 0)
        {
            foreground = Color.Green3;
        }

        return new Style(foreground: foreground, background: background);
    }

    private IRenderable GetGapBetweenLines(TimingDataPoint.Driver? from, TimingDataPoint.Driver to)
    {
        if (from == to)
        {
            return new Text("-------", _invert);
        }

        if (from?.GapToLeaderSeconds() is not null && to.GapToLeaderSeconds() is not null)
        {
            var gap = to.GapToLeaderSeconds() - from.GapToLeaderSeconds();
            return new Text($"{(gap > 0 ? "+" : "")}{gap, 3}".PadLeft(7));
        }

        return new Text("");
    }

    private string GetPositionChangeMarkup(int? change) =>
        change switch
        {
            < 0 => "[green]▲[/]",
            > 0 => "[yellow]▼[/]",
            _ => ""
        };

    private IRenderable GetRaceControlPanel()
    {
        var table = new Table();
        table.NoBorder();
        table.Expand();
        table.HideHeaders();
        table.AddColumns("Message");

        var messages = raceControlMessages
            .Latest.Messages.OrderByDescending(x => x.Value.Utc)
            .Skip(state.CursorOffset)
            .Take(5);

        foreach (var (key, value) in messages)
        {
            table.AddRow($"► {value.Message}");
        }
        return new Panel(table)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true,
            Border = BoxBorder.Rounded
        };
    }

    private IRenderable GetComparisonPanel()
    {
        var lines = timingData.Latest.Lines;

        var previousLine = lines.FirstOrDefault(x => x.Value.Line == state.CursorOffset - 1);
        var selectedLine = lines.FirstOrDefault(x => x.Value.Line == state.CursorOffset);
        var nextLine = lines.FirstOrDefault(x => x.Value.Line == state.CursorOffset + 1);

        var charts = new List<BarChart>();

        if (previousLine.Key is not null)
        {
            var chart = GetComparisonChart(previousLine.Key, selectedLine.Key, selectedLine.Value);
            if (chart is not null)
                charts.Add(chart);
        }

        if (nextLine.Key is not null)
        {
            var chart = GetComparisonChart(selectedLine.Key, nextLine.Key, nextLine.Value);
            if (chart is not null)
                charts.Add(chart);
        }

        return new Columns(charts).PadLeft(1).Expand();
    }

    private BarChart? GetComparisonChart(
        string prevDriverNumber,
        string nextDriverNumber,
        TimingDataPoint.Driver nextLine
    )
    {
        if (
            string.IsNullOrWhiteSpace(prevDriverNumber)
            || string.IsNullOrWhiteSpace(nextDriverNumber)
        )
            return null;
        var prevDriver = driverList.Latest?.GetValueOrDefault(prevDriverNumber) ?? new();
        var nextDriver = driverList.Latest?.GetValueOrDefault(nextDriverNumber) ?? new();
        var currentLap = nextLine.NumberOfLaps;
        if (!currentLap.HasValue)
            return null;

        var chart = new BarChart()
            .Width(((System.Console.WindowWidth - STATUS_PANEL_WIDTH) / 2) - 1)
            .Label(
                $"[#{prevDriver.TeamColour} bold]{prevDriver.Tla}[/] [italic]vs[/] [#{nextDriver.TeamColour} bold]{nextDriver.Tla}[/]"
            );

        if (currentLap.Value > 0)
        {
            AddChartItem(chart, currentLap.Value, prevDriverNumber, nextDriverNumber);
        }
        else
        {
            return null;
        }

        if (currentLap.Value - 1 > 0)
        {
            AddChartItem(chart, currentLap.Value - 1, prevDriverNumber, nextDriverNumber);
        }

        if (currentLap.Value - 2 > 0)
        {
            AddChartItem(chart, currentLap.Value - 2, prevDriverNumber, nextDriverNumber);
        }

        if (currentLap.Value - 3 > 0)
        {
            AddChartItem(chart, currentLap.Value - 3, prevDriverNumber, nextDriverNumber);
        }
        return chart;
    }

    private void AddChartItem(
        BarChart chart,
        int lapNumber,
        string prevDriverNumber,
        string nextDriverNumber
    )
    {
        var gap = GetGapBetweenDriversOnLap(lapNumber, prevDriverNumber, nextDriverNumber);
        var gapOnPreviousLap = GetGapBetweenDriversOnLap(
            lapNumber - 1,
            prevDriverNumber,
            nextDriverNumber
        );
        var color = gap switch
        {
            _ when gap > gapOnPreviousLap => Color.Red,
            _ when gap < gapOnPreviousLap => Color.Green,
            _ => Color.Silver
        };
        chart.AddItem($"LAP {lapNumber}", (double)gap, color);
    }

    private decimal GetGapBetweenDriversOnLap(
        int lapNumber,
        string prevDriverNumber,
        string nextDriverNumber
    )
    {
        var prevGapToLeader = timingData
            .DriversByLap.GetValueOrDefault(lapNumber)
            ?.GetValueOrDefault(prevDriverNumber)
            ?.GapToLeaderSeconds();
        var nextGapToLeader = timingData
            .DriversByLap.GetValueOrDefault(lapNumber)
            ?.GetValueOrDefault(nextDriverNumber)
            ?.GapToLeaderSeconds();
        return nextGapToLeader - prevGapToLeader ?? 0;
    }

    private IRenderable GetStatusPanel()
    {
        var items = new List<IRenderable>();

        if (trackStatusProcessor.Latest is not null)
        {
            var style = trackStatusProcessor.Latest.Status switch
            {
                "1" => _personalBest, // All Clear
                "2" => new Style(foreground: Color.Black, background: Color.Yellow), // Yellow Flad
                "4" => new Style(foreground: Color.Black, background: Color.Yellow), // Safety Car
                "5" => new Style(foreground: Color.White, background: Color.Red), // Red Flag
                _ => Style.Plain
            };
            items.Add(
                new Text(
                    $"{trackStatusProcessor.Latest.Status} {trackStatusProcessor.Latest.Message}",
                    style
                )
            );
        }

        items.Add(new Text($@"Delayed By"));
        items.Add(new Text($@"{timingService.Delay:d\.hh\:mm\:ss}"));

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Status"),
            Expand = true,
            Border = BoxBorder.Rounded
        };
    }
}
