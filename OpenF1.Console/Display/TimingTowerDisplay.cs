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
    ITimingService timingService,
) : IDisplay
{
    public Screen Screen => Screen.TimingTower;

    private readonly Style _personalBest = new(foreground: Color.Black, background: Color.Green);
    private readonly Style _overallBest = new(foreground: Color.Black, background: Color.Purple);
    private readonly Style _normal = new(foreground: Color.White);
    private readonly Style _invert = new(foreground: Color.Black, background: Color.White);

    public Task<IRenderable> GetContentAsync()
    {
        var statusPanel = GetStatusPanel();
        var raceControlPanel = GetRaceControlPanel();
        var timingTower = sessionInfoProcessor.Latest.IsRace()
            ? GetRaceTimingTower()
            : GetNonRaceTimingTower();

        var layout = new Layout("Root").SplitRows(
            new Layout("Timing Tower", timingTower),
            new Layout("Info").SplitColumns(
                new Layout("Status", statusPanel),
                new Layout("Race Control Messages", raceControlPanel)
            )
        );

        layout["Info"].Size = 5;
        layout["Info"]["Status"].Size = 19;

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetRaceTimingTower()
    {
        if (timingData.LatestLiveTimingDataPoint is null)
            return new Text("No Timing");

        var table = new Table();
        table.AddColumns(
            $"LAP {lapCountProcessor.Latest?.CurrentLap, 2}/{lapCountProcessor.Latest?.TotalLaps}",
            "Gap",
            "Interval",
            "Best Lap",
            "Last Lap",
            "S1",
            "S2",
            "S3",
            "Pit",
            "Tyre",
            "Compare"
        );

        var comparisonDataPoint = timingData.LatestLiveTimingDataPoint.Lines.FirstOrDefault(x =>
            x.Value.Line == state.CursorOffset
        );

        foreach (var (driverNumber, line) in timingData.LatestLiveTimingDataPoint.GetOrderedLines())
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var teamColour = driver.TeamColour ?? "000000";

            var isComparisonLine = line == comparisonDataPoint.Value;
            var lineStyle = isComparisonLine ? _invert : _normal;

            table.AddRow(
                new Markup(
                    $"{line.Position, 2} [#{teamColour}]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]",
                    lineStyle
                ),
                new Text($"{line.GapToLeader, 7}", lineStyle),
                new Text(
                    $"{line.IntervalToPositionAhead?.Value, 8}",
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
                GetGapBetweenLines(comparisonDataPoint.Value, line)
            );
        }

        table.NoBorder();

        return table;
    }

    private IRenderable GetNonRaceTimingTower()
    {
        if (timingData.LatestLiveTimingDataPoint is null)
            return new Text("No Timing");

        var table = new Table();
        table.AddColumns(
            sessionInfoProcessor.Latest.Name ?? "Unknown Session",
            "Gap",
            "Best Lap",
            "BS1",
            "BS2",
            "BS3",
            "S1",
            "S2",
            "S3",
            "Pit",
            "Tyre"
        );

        var bestDriver = timingData.LatestLiveTimingDataPoint.GetOrderedLines().First();

        var bestSector1 = timingData
            .BestLaps.DefaultIfEmpty()
            .Min(x => x.Value?.Sectors.GetValueOrDefault("0")?.ToTimeSpan() ?? TimeSpan.MaxValue);
        var bestSector2 = timingData
            .BestLaps.DefaultIfEmpty()
            .Min(x => x.Value?.Sectors.GetValueOrDefault("1")?.ToTimeSpan() ?? TimeSpan.MaxValue);
        var bestSector3 = timingData
            .BestLaps.DefaultIfEmpty()
            .Min(x => x.Value?.Sectors.GetValueOrDefault("2")?.ToTimeSpan() ?? TimeSpan.MaxValue);

        foreach (var (driverNumber, line) in timingData.LatestLiveTimingDataPoint.GetOrderedLines())
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
                    $"{line.Position, 2} [#{teamColour}]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]",
                    _normal
                ),
                new Text($"{(gapToLeader > 0 ? "+" : "")}{gapToLeader:f3}".PadLeft(7), _normal),
                new Text(line.BestLapTime?.Value ?? "NULL", _normal),
                new Text(
                    bestLap?.Sectors.GetValueOrDefault("0")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(bestLap?.Sectors.GetValueOrDefault("0"), bestSector1)
                ),
                new Text(
                    bestLap?.Sectors.GetValueOrDefault("1")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(bestLap?.Sectors.GetValueOrDefault("1"), bestSector2)
                ),
                new Text(
                    bestLap?.Sectors.GetValueOrDefault("2")?.Value?.PadLeft(6) ?? "      ",
                    GetStyle(bestLap?.Sectors.GetValueOrDefault("2"), bestSector3)
                ),
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
                new Text($"{stint?.Compound?[0]} {stint?.TotalLaps, 2}", GetStyle(stint))
            );
        }

        table.NoBorder();

        return table;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time, TimeSpan? bestSector = null)
    {
        if (string.IsNullOrWhiteSpace(time?.Value))
            return _normal;

        if (bestSector == time?.ToTimeSpan())
            return _overallBest;

        // If we are checking against a best sector, don't style it unless it is fasted
        if (bestSector.HasValue)
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
            "SOFT" => new Style(foreground: Color.Black, background: Color.Red),
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

        if (interval.IntervalSeconds() < 1)
        {
            foreground = Color.Green3;
        }

        return new Style(foreground: foreground, background: background);
    }

    private IRenderable GetGapBetweenLines(TimingDataPoint.Driver? from, TimingDataPoint.Driver to)
    {
        if (from == to)
        {
            return new Text("-------");
        }

        if (from?.GapToLeaderSeconds() is not null && to.GapToLeaderSeconds() is not null)
        {
            var gap = to.GapToLeaderSeconds() - from.GapToLeaderSeconds();
            return new Text($"{(gap > 0 ? "+" : "")}{gap, 3}".PadLeft(7));
        }

        return new Text("");
    }

    private IRenderable GetRaceControlPanel()
    {
        var table = new Table();
        table.NoBorder();
        table.Expand();
        table.HideHeaders();
        table.AddColumns("Timestamp", "Message");

        var messages = raceControlMessages
            .RaceControlMessages.Messages.OrderByDescending(x => x.Value.Utc)
            .Skip(state.CursorOffset)
            .Take(5);

        foreach (var (key, value) in messages)
        {
            table.AddRow($"{value.Utc:HH:mm:ss}", value.Message);
        }
        return new Panel(table)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true
        };
    }

    private IRenderable GetStatusPanel()
    {
        var items = new List<IRenderable>();

        if (trackStatusProcessor.Latest is not null)
        {
            var style = trackStatusProcessor.Latest.Status switch
            {
                "1" => new Style(background: Color.Green),
                "2" => new Style(background: Color.Yellow),
                "4" => new Style(background: Color.Yellow),
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
        return new Panel(rows) { Header = new PanelHeader("Status"), Expand = true };
    }
}
