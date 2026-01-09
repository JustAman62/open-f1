using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public class RaceControlDisplay(
    State state,
    ExtrapolatedClockProcessor extrapolatedClockProcessor,
    RaceControlMessageProcessor raceControlMessages,
    TrackStatusProcessor trackStatusProcessor,
    LapCountProcessor lapCountProcessor,
    WeatherProcessor weatherProcessor,
    SessionInfoProcessor sessionInfo,
    IDateTimeProvider dateTimeProvider
) : IDisplay
{
    public Screen Screen => Screen.RaceControl;

    public Task<IRenderable> GetContentAsync()
    {
        var raceControlPanel = GetRaceControlPanel();
        var statusPanel = GetStatusPanel();
        var clockPanel = GetClockPanel();
        var weatherPanel = GetWeatherPanel();

        var layout = new Layout("Root").SplitColumns(
            new Layout("Race Control Messages", raceControlPanel),
            new Layout("Info")
                .SplitRows(
                    new Layout("Status", statusPanel).Size(6),
                    new Layout("Clock", clockPanel).Size(13),
                    new Layout("Weather", weatherPanel)
                )
                .Size(23)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetRaceControlPanel()
    {
        var table = new Table();
        table.NoBorder();
        table.Expand();
        table.HideHeaders();
        table.AddColumns("Timestamp", "Message");

        if (state.CursorOffset > 0)
        {
            table.AddRow(
                new Text(string.Empty),
                new Text(
                    $"Skipping {state.CursorOffset} messages",
                    new Style(foreground: Color.Red)
                )
            );
        }

        var messages = raceControlMessages
            .Latest.Messages.OrderByDescending(x => x.Value.Utc)
            .Skip(state.CursorOffset)
            .Take(20);

        foreach (var (key, value) in messages)
        {
            table.AddRow($"{value.Utc:HH:mm:ss}", value.Message ?? "MISSING");
        }
        return new Panel(table)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true,
            Border = BoxBorder.Rounded,
        };
    }

    private IRenderable GetStatusPanel()
    {
        var lapCount =
            $"LAP {lapCountProcessor.Latest?.CurrentLap, 2}/{lapCountProcessor.Latest?.TotalLaps, 2}";
        var items = new List<IRenderable> { new Text(lapCount) };

        if (trackStatusProcessor.Latest is not null)
        {
            var style = trackStatusProcessor.Latest.Status switch
            {
                "1" => DisplayUtils.STYLE_PB, // All Clear
                "2" => new Style(foreground: Color.Black, background: Color.Yellow), // Yellow Flag
                "4" => new Style(foreground: Color.Black, background: Color.Yellow), // Safety Car
                "6" => new Style(foreground: Color.Black, background: Color.Yellow), // VSC Deployed
                "7" => new Style(foreground: Color.Black, background: Color.Yellow), // VSC Ending
                "5" => new Style(foreground: Color.White, background: Color.Red), // Red Flag
                _ => Style.Plain,
            };
            items.Add(
                new Text(
                    $"{trackStatusProcessor.Latest.Status} {trackStatusProcessor.Latest.Message}",
                    style
                )
            );
        }

        items.AddRange([
            new Text(sessionInfo.Latest.Meeting?.Circuit?.ShortName ?? string.Empty),
            new Text(sessionInfo.Latest.Name ?? string.Empty),
        ]);

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Status"),
            Expand = true,
            Border = BoxBorder.Rounded,
        };
    }

    private IRenderable GetClockPanel()
    {
        var items = new List<IRenderable>
        {
            new Text($"Simulation Time"),
            new Text($"{dateTimeProvider.Utc:s}"),
            new Text($@"Delayed By"),
            new Text($@"{dateTimeProvider.Delay:d\.hh\:mm\:ss}"),
            new Text($@"Session Clock"),
            new Text($@"{extrapolatedClockProcessor.ExtrapolatedRemaining():hh\:mm\:ss}"),
            new Text(string.Empty),
            new Text($@"Start (UTC)"),
            new Text($@"{sessionInfo.Latest.GetStartDateTimeUtc():s}"),
            new Text($@"Start (Local)"),
            new Text($@"{sessionInfo.Latest.StartDate:s}"),
        };

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Clock"),
            Expand = true,
            Border = BoxBorder.Rounded,
        };
    }

    private IRenderable GetWeatherPanel()
    {
        var weather = weatherProcessor.Latest;
        var items = new List<IRenderable>
        {
            // new Markup($"{Emoji.Known.Thermometer} Air   {weather?.AirTemp}C"),
            // new Markup($"{Emoji.Known.Thermometer} Track {weather?.TrackTemp}C"),
            // new Markup($"{Emoji.Known.DashingAway} {weather?.WindSpeed}kph"),
            // new Markup($"{Emoji.Known.CloudWithRain}  {weather?.Rainfall}mm"),
            new Markup($"Air   {weather?.AirTemp}C"),
            new Markup($"Track {weather?.TrackTemp}C"),
            new Markup($"Wind  {weather?.WindSpeed}kph"),
            new Markup($"Rain  {weather?.Rainfall}mm"),
        };

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Weather"),
            Expand = true,
            Border = BoxBorder.Rounded,
        };
    }
}
