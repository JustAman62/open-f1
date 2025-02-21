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
            new Layout("Timing Tower", timingTower),
            new Layout("Info")
                .SplitColumns(
                    new Layout("Status", statusPanel).Size(STATUS_PANEL_WIDTH),
                    new Layout("Race Control Messages", raceControlPanel)
                )
                .Size(6)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetRaceTimingTower()
    {
        var lap = lapCountProcessor.Latest;
        var table = new Table();
        table
            .AddColumns(
                new TableColumn($"LAP {lap?.CurrentLap, 2}/{lap?.TotalLaps}")
                {
                    Width = 9,
                    Alignment = Justify.Left,
                },
                new TableColumn("Leader") { Width = 8, Alignment = Justify.Right },
                new TableColumn("Gap") { Width = 8, Alignment = Justify.Right },
                new TableColumn("Best Lap") { Width = 9, Alignment = Justify.Right },
                new TableColumn("Last Lap") { Width = 9, Alignment = Justify.Right },
                new TableColumn("S1") { Width = 7, Alignment = Justify.Right },
                new TableColumn("S2") { Width = 7, Alignment = Justify.Right },
                new TableColumn("S3") { Width = 7, Alignment = Justify.Right },
                new TableColumn("Pit") { Width = 4, Alignment = Justify.Right },
                new TableColumn("Tyre") { Width = 5, Alignment = Justify.Right },
                new TableColumn(" Compare ") { Width = 9, Alignment = Justify.Right },
                new TableColumn("Driver") { Width = 9 },
                new TableColumn("") { Width = 1 }
            )
            .NoBorder()
            .NoSafeBorder()
            .RemoveColumnPadding();

        var comparisonDataPoint = timingData.Latest.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );

        var fastestLastLap = timingData
            .Latest.Lines.Values.MinBy(x => x.LastLapTime?.ToTimeSpan())
            ?.LastLapTime;
        var fastestBestLap = timingData
            .Latest.Lines.Values.MinBy(x => x.BestLapTime?.ToTimeSpan())
            ?.BestLapTime;

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

            if (line.Retired.GetValueOrDefault())
            {
                table.AddRow(DisplayUtils.DriverTag(driver, line, selected: false));
                continue;
            }

            table.AddRow(
                DisplayUtils.DriverTag(driver, line, isComparisonLine),
                new Text(
                    $"{line.GapToLeader}".ToFixedWidth(8),
                    isComparisonLine ? DisplayUtils.STYLE_INVERT : DisplayUtils.STYLE_NORMAL
                ),
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack
                    ? new Text("OFF TRK", new Style(background: Color.Red, foreground: Color.White))
                    : new Text(
                        $"{(car?.Channels.Drs >= 8 ? "•" : "")}{line.IntervalToPositionAhead?.Value}".ToFixedWidth(
                            8
                        ),
                        DisplayUtils.GetStyle(line.IntervalToPositionAhead, isComparisonLine, car)
                    ),
                new Text(
                    line.BestLapTime?.Value ?? "NULL",
                    GetStyle(line.BestLapTime, fastestBestLap)
                        .Combine(new Style(decoration: Decoration.Dim))
                ),
                new Text(
                    line.LastLapTime?.Value ?? "NULL",
                    GetStyle(line.LastLapTime, fastestLastLap)
                ),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("0")?.Value}".ToFixedWidth(6),
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("1")?.Value}".ToFixedWidth(6),
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    $"{line.Sectors.GetValueOrDefault("2")?.Value}".ToFixedWidth(6),
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                ),
                new Text(
                    line.InPit.GetValueOrDefault() ? "IN "
                        : line.PitOut.GetValueOrDefault() ? "OUT"
                        : $"{line.NumberOfPitStops, 2}",
                    line.InPit.GetValueOrDefault()
                            ? new Style(foreground: Color.Black, background: Color.Yellow)
                        : line.PitOut.GetValueOrDefault()
                            ? new Style(foreground: Color.Black, background: Color.Green)
                        : Style.Plain
                ),
                new Text($"{stint?.Compound?[0]} {stint?.TotalLaps, 2}", GetStyle(stint)),
                DisplayUtils.GetGapBetweenLines(comparisonDataPoint.Value, line),
                DisplayUtils.DriverTag(driver, line, isComparisonLine),
                new Markup(GetPositionChangeMarkup(positionChange))
            );
        }

        return table;
    }

    private IRenderable GetNonRaceTimingTower()
    {
        if (timingData.Latest is null)
            return new Text("No Timing Available");

        var table = new Table()
            .AddColumns(
                new TableColumn(sessionInfoProcessor.Latest.Name?.ToFixedWidth(9) ?? "Unknown ")
                {
                    Width = 10,
                    Alignment = Justify.Left,
                },
                new TableColumn("Gap") { Width = 8, Alignment = Justify.Left },
                new TableColumn("Best Lap") { Width = 9, Alignment = Justify.Left },
                new TableColumn("BL S1") { Width = 15, Alignment = Justify.Left },
                new TableColumn("BL S2") { Width = 15, Alignment = Justify.Left },
                new TableColumn("BL S3") { Width = 15, Alignment = Justify.Left },
                new TableColumn("S1") { Width = 7, Alignment = Justify.Left },
                new TableColumn("S2") { Width = 7, Alignment = Justify.Left },
                new TableColumn("S3") { Width = 7, Alignment = Justify.Left },
                new TableColumn("Pit") { Width = 4, Alignment = Justify.Left },
                new TableColumn("Tyre") { Width = 5, Alignment = Justify.Left }
            )
            .NoBorder()
            .RemoveColumnPadding();

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

            if (line.KnockedOut.GetValueOrDefault())
            {
                table.AddRow(DisplayUtils.DriverTag(driver, line, selected: false));
                continue;
            }

            table.AddRow(
                DisplayUtils.DriverTag(driver, line, selected: false),
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack
                    ? new Text("OFF TRK", new Style(background: Color.Red, foreground: Color.White))
                    : new Text(
                        $"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".ToFixedWidth(7),
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
                    line.InPit.GetValueOrDefault() ? "IN "
                        : line.PitOut.GetValueOrDefault() ? "OUT"
                        : $"{line.NumberOfPitStops, 2}",
                    line.InPit.GetValueOrDefault()
                            ? new Style(foreground: Color.Black, background: Color.Yellow)
                        : line.PitOut.GetValueOrDefault() ? DisplayUtils.STYLE_PB
                        : Style.Plain
                ),
                new Text($"{stint?.Compound?[0] ?? 'X'} {stint?.TotalLaps, 2}", GetStyle(stint))
            );
        }
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
            $"[{GetStyle(time, bestSector, overrideStyle).ToMarkup()}]{time?.Value?.ToFixedWidth(6)}[/]";

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

    private Style GetStyle(
        TimingDataPoint.Driver.BestLap? time,
        TimingDataPoint.Driver.BestLap? bestLap = null
    ) =>
        bestLap?.ToTimeSpan() == time?.ToTimeSpan()
            ? DisplayUtils.STYLE_BEST
            : DisplayUtils.STYLE_NORMAL;

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
            "INTERMEDIATE" => new Style(foreground: Color.Black, background: Color.Green),
            "WET" => new Style(foreground: Color.Black, background: Color.Blue),
            _ => DisplayUtils.STYLE_NORMAL,
        };
    }

    private string GetPositionChangeMarkup(int? change) =>
        change switch
        {
            < 0 => "[green]▲[/]",
            > 0 => "[yellow]▼[/]",
            _ => "",
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
            Border = BoxBorder.Rounded,
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
            .Width(((Terminal.Size.Width - STATUS_PANEL_WIDTH) / 2) - 1)
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
            _ => Color.Silver,
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
                "2" => new Style(foreground: Color.Black, background: Color.Yellow), // Yellow Flag
                "4" => new Style(foreground: Color.Black, background: Color.Yellow), // Safety Car
                "6" => new Style(foreground: Color.Black, background: Color.Yellow), // VSC Deployed
                "5" => new Style(foreground: Color.White, background: Color.Red), // Red Flag
                _ => Style.Plain,
            };
            items.Add(new Text($"{trackStatusProcessor.Latest.Message}", style));
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
}
