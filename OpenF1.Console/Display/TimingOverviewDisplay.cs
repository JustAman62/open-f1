using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingOverviewDisplay(RaceControlMessageProcessor raceControlMessages) : IDisplay
{
    public Screen Screen => Screen.TimingOverview;

    public Task<IRenderable> GetContentAsync()
    {
        var raceControlRows = new Rows(
            raceControlMessages
                .RaceControlMessages
                .Messages
                .OrderByDescending(x => x.Value.Utc)
                .Select(x => new Text(
                    $"{x.Value.Utc} {x.Value.Message}"
                ))
                .Take(4)
        );
        var raceControlPanel = new Panel(raceControlRows)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true
        };

        return Task.FromResult<IRenderable>(raceControlPanel);
    }
}
