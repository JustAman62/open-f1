using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public sealed class TeamRadioDisplay(
    State state,
    TeamRadioProcessor teamRadio,
    ChampionshipPredictionProcessor championshipPrediction,
    DriverListProcessor driverList
) : IDisplay
{
    public Screen Screen => Screen.SessionStats;

    public Task<IRenderable> GetContentAsync()
    {
        var layout = new Layout("Root").SplitColumns(
            new Layout("Left").SplitRows(
                new Layout("Top Left", GetTeamsChampionshipTable()) { Size = 15 },
                new Layout("Bottom Left", GetTeamRadioTable())
            ),
            new Layout("Right", GetDriversChampionshipTable())
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetTeamRadioTable()
    {
        var table = new Table();
        table.AddColumns(
            new TableColumn("Idx") { Width = 2, Alignment = Justify.Right },
            new TableColumn("Time") { Width = 8, Alignment = Justify.Center },
            new TableColumn("Driver") { Width = 6, Alignment = Justify.Center },
            new TableColumn("Action")
        );
        table.Expand();
        table.RoundedBorder();
        table.Title = new TableTitle("Team Radio");

        var selectedIdx = teamRadio.Ordered.ElementAtOrDefault(state.CursorOffset).Key;

        foreach (var (idx, entry) in teamRadio.Ordered)
        {
            var driver = driverList.Latest!.GetValueOrDefault(entry.RacingNumber) ?? new();
            table.AddRow(
                new Text($"{idx, 2}"),
                new Text($"{entry.Utc:HH\\:mm\\:ss}"),
                new Markup(DisplayUtils.MarkedUpDriverNumber(driver)),
                idx == selectedIdx
                    ? new Text("► Play", DisplayUtils.STYLE_INVERT)
                    : new Text(string.Empty)
            );
        }
        return table;
    }

    private IRenderable GetDriversChampionshipTable()
    {
        var table = new Table();
        table.AddColumns(
            new TableColumn("Pos") { Width = 2, Alignment = Justify.Right },
            new TableColumn("Driver") { Width = 6, Alignment = Justify.Right },
            new TableColumn("Points")
        );
        table.Expand();
        table.RoundedBorder();
        table.Title = new TableTitle("Drivers Championship");

        foreach (var (driverNumber, data) in championshipPrediction.Latest.Drivers)
        {
            var driver = driverList.Latest.GetValueOrDefault(
                driverNumber,
                new DriverListDataPoint.Driver() { RacingNumber = driverNumber }
            );
            var points = data.PredictedPoints.HasValue
                ? data.PredictedPoints.Value.ToString("###.#").PadLeft(5)
                : string.Empty;
            table.AddRow(
                new Text($"{data.PredictedPosition, 2}"),
                new Markup(DisplayUtils.MarkedUpDriverNumber(driver)),
                new Text(points)
            );
        }

        return table;
    }

    private IRenderable GetTeamsChampionshipTable()
    {
        var table = new Table();
        table.AddColumns(
            new TableColumn("Pos") { Width = 2, Alignment = Justify.Right },
            new TableColumn("Team"),
            new TableColumn("Points") { Width = 5, Alignment = Justify.Right }
        );
        table.Expand();
        table.RoundedBorder();
        table.Title = new TableTitle("Constructors Championship");

        foreach (var (teamName, data) in championshipPrediction.Latest.Teams)
        {
            var driver = driverList
                .Latest.FirstOrDefault(x => x.Value.TeamName == data.TeamName)
                .Value;
            var points = data.PredictedPoints.HasValue
                ? data.PredictedPoints.Value.ToString("###.#").PadLeft(5)
                : string.Empty;
            table.AddRow(
                new Text($"{data.PredictedPosition, 3}"),
                new Markup($"[#{driver.TeamColour ?? "000000"} bold]{teamName}[/]"),
                new Text(points)
            );
        }

        return table;
    }
}
