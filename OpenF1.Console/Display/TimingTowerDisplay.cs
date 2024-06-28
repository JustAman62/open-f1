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
    ExtrapolatedClockProcessor extrapolatedClock,
    PositionDataProcessor positionData,
    CarDataProcessor carData,
    IDateTimeProvider dateTimeProvider
) : IDisplay
{
    public Screen Screen => Screen.TimingTower;

    private const int STATUS_PANEL_WIDTH = 15;

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

        layout["Info"].Size = 6;
        layout["Info"]["Status"].Size = STATUS_PANEL_WIDTH;

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetRaceTimingTower()
    {
        var table = new Table();
        var lap = lapCountProcessor.Latest;
        table
            .AddColumns(
                new TableColumn($"LAP {lap?.CurrentLap, 2}/{lap?.TotalLaps}"),
                new TableColumn("Leader") { Width = 6, Alignment = Justify.Right },
                new TableColumn("Gap") { Width = 6, Alignment = Justify.Right },
                new TableColumn("Best Lap"),
                new TableColumn("Last Lap"),
                new TableColumn("S1") { Width = 6 },
                new TableColumn("S2") { Width = 6 },
                new TableColumn("S3") { Width = 6 },
                new TableColumn("Pit"),
                new TableColumn("Tyre"),
                new TableColumn("Compare"),
                new TableColumn("Driver"),
                new TableColumn("") { Width = 1 }
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
            var position =
                positionData
                    .Latest.Position.LastOrDefault()
                    ?.Entries.GetValueOrDefault(driverNumber) ?? new();
            var car = carData.Latest.Entries.FirstOrDefault()?.Cars.GetValueOrDefault(driverNumber);
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;

            // Driver.Line is only updated at the end of the lap, so if they are different a position change has just happened
            var positionChange = line.Line - driver.Line;

            var isComparisonLine = line == comparisonDataPoint.Value;

            table.AddRow(
                DisplayUtils.DriverTag(driver, line, isComparisonLine),
                new Text(
                    $"{line.GapToLeader}",
                    isComparisonLine ? DisplayUtils.STYLE_INVERT : DisplayUtils.STYLE_NORMAL
                ),
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack
                    ? new Text("OFF TRK", new Style(background: Color.Red, foreground: Color.White))
                    : new Text(
                        $"{(car?.Channels.Drs >= 8 ? "•" : "")}{line.IntervalToPositionAhead?.Value}",
                        GetStyle(line.IntervalToPositionAhead, isComparisonLine, car)
                    ),
                new Text(line.BestLapTime?.Value ?? "NULL", DisplayUtils.STYLE_NORMAL),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("0")?.Value}",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("1")?.Value}",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("2")?.Value}",
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
                DisplayUtils.DriverTag(driver, line, isComparisonLine),
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
                "Gap",
                "Best Lap",
                "BL S1",
                "BL S2",
                "BL S3",
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
            var position =
                positionData
                    .Latest.Position.LastOrDefault()
                    ?.Entries.GetValueOrDefault(driverNumber) ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var bestLap = timingData.BestLaps.GetValueOrDefault(driverNumber);

            var gapToLeader = (
                line.BestLapTime.ToTimeSpan() - bestDriver.Value.BestLapTime.ToTimeSpan()
            )?.TotalSeconds;

            table.AddRow(
                DisplayUtils.DriverTag(driver, line, selected: false),
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack
                    ? new Text("OFF TRK", new Style(background: Color.Red, foreground: Color.White))
                    : new Text(
                        $"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".PadLeft(7),
                        DisplayUtils.STYLE_NORMAL
                    ),
                new Text(line.BestLapTime?.Value ?? "NULL", DisplayUtils.STYLE_NORMAL),
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
                            ? DisplayUtils.STYLE_PB
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
            return DisplayUtils.STYLE_NORMAL;

        if (bestSector?.ToTimeSpan() == time?.ToTimeSpan())
            return DisplayUtils.STYLE_BEST;

        // If we are checking against a best sector, don't style it unless it is fastest
        if (overrideStyle)
            return DisplayUtils.STYLE_NORMAL;

        if (time?.OverallFastest ?? false)
            return DisplayUtils.STYLE_BEST;
        if (time?.PersonalFastest ?? false)
            return DisplayUtils.STYLE_PB;
        return DisplayUtils.STYLE_NORMAL;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingAppDataPoint.Driver.Stint? stint)
    {
        if (stint is null)
            return DisplayUtils.STYLE_NORMAL;

        return stint.Compound switch
        {
            "HARD" => new Style(foreground: Color.White, background: Color.Grey),
            "MEDIUM" => new Style(foreground: Color.Black, background: Color.Yellow),
            "SOFT" => new Style(foreground: Color.White, background: Color.Red),
            "INTER" => new Style(foreground: Color.Black, background: Color.Green),
            "WET" => new Style(foreground: Color.Black, background: Color.Blue),
            _ => DisplayUtils.STYLE_NORMAL
        };
    }

    private Style GetStyle(
        TimingDataPoint.Driver.Interval? interval,
        bool isComparisonLine,
        CarDataPoint.Entry.Car? car = null
    )
    {
        if (interval is null)
            return DisplayUtils.STYLE_NORMAL;

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

        if (car is { Channels: { Drs: > 8 } })
        {
            foreground = Color.White;
            background = Color.Green3;
        }
        return new Style(foreground: foreground, background: background);
    }

    private IRenderable GetGapBetweenLines(TimingDataPoint.Driver? from, TimingDataPoint.Driver to)
    {
        if (from == to)
        {
            return new Text("-------", DisplayUtils.STYLE_INVERT);
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

        if (currentLap.Value - 4 > 0)
        {
            AddChartItem(chart, currentLap.Value - 4, prevDriverNumber, nextDriverNumber);
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
                "1" => DisplayUtils.STYLE_PB, // All Clear
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

        items.Add(new Text($@"{dateTimeProvider.Utc:HH\:mm\:ss}"));
        items.Add(new Text($@"{extrapolatedClock.ExtrapolatedRemaining():hh\:mm\:ss}"));

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Status"),
            Expand = true,
            Border = BoxBorder.Rounded
        };
    }
}
