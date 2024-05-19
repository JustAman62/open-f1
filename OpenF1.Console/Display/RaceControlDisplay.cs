using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class RaceControlDisplay(
    State state,
    ExtrapolatedClockProcessor extrapolatedClockProcessor,
    RaceControlMessageProcessor raceControlMessages,
    TrackStatusProcessor trackStatusProcessor,
    LapCountProcessor lapCountProcessor,
    WeatherProcessor weatherProcessor,
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
            new Layout("Info").SplitRows(
                new Layout("Status", statusPanel),
                new Layout("Clock", clockPanel),
                new Layout("Weather", weatherPanel)
            )
        );

        layout["Info"].Size = 23;
        layout["Info"]["Status"].Size = 4;
        layout["Info"]["Clock"].Size = 8;

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
            table.AddRow($"{value.Utc:HH:mm:ss}", value.Message);
        }
        return new Panel(table)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true,
            Border = BoxBorder.Rounded
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

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Status"),
            Expand = true,
            Border = BoxBorder.Rounded
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
            new Text($@"{extrapolatedClockProcessor.ExtrapolatedRemaining():hh\:mm\:ss}")
        };

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Clock"),
            Expand = true,
            Border = BoxBorder.Rounded
        };
    }

    private IRenderable GetWeatherPanel()
    {
        var weather = weatherProcessor.Latest;
        var items = new List<IRenderable>
        {
            new Markup($"{Emoji.Known.Thermometer} Air   {weather?.AirTemp}C"),
            new Markup($"{Emoji.Known.Thermometer} Track {weather?.TrackTemp}C"),
            new Markup($"{Emoji.Known.DashingAway} {weather?.WindSpeed}kph"),
            new Markup($"{Emoji.Known.CloudWithRain}  {weather?.Rainfall}mm"),
        };

        var rows = new Rows(items);
        return new Panel(rows)
        {
            Header = new PanelHeader("Weather"),
            Expand = true,
            Border = BoxBorder.Rounded
        };
    }
}
