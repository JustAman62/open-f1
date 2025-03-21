using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public sealed class TeamRadioDisplay(
    State state,
    TeamRadioProcessor teamRadio,
    DriverListProcessor driverList
) : IDisplay
{
    public Screen Screen => Screen.TeamRadio;

    public Task<IRenderable> GetContentAsync()
    {
        var layout = new Layout("Root").SplitColumns(
            new Layout("Left", GetTeamRadioTable()) { Size = 28 },
            new Layout("Right", GetSelectedTranscription())
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
        table.NoBorder().RemoveColumnPadding();

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
        return new Panel(table)
        {
            Header = new PanelHeader("Select Team Radio"),
            Expand = true,
            Padding = new Padding(0),
        };
    }

    private IRenderable GetSelectedTranscription()
    {
        var selected = teamRadio.Ordered.ElementAtOrDefault(state.CursorOffset);

        var text = string.IsNullOrWhiteSpace(selected.Value?.Transcription)
            ? new Text("No transcription loaded. Press [T] to load.")
            : new Text(selected.Value.Transcription);

        return new Panel(text) { Expand = true, Header = new PanelHeader("Transcription") };
    }
}
