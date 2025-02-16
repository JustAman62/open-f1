using Microsoft.Extensions.Options;
using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public record StartSimulatedSessionOptions
{
    public Dictionary<
        (string Location, DateOnly Date),
        List<(string Session, string Directory)>
    >? Sessions = null;
    public int? SelectedLocation = null;
}

public class StartSimulatedSessionDisplay(
    State state,
    StartSimulatedSessionOptions displayOptions,
    IOptions<LiveTimingOptions> options
) : IDisplay
{
    public Screen Screen => Screen.StartSimulatedSession;

    public Task<IRenderable> GetContentAsync()
    {
        var directories = displayOptions.Sessions;
        if (directories is null)
        {
            return Task.FromResult<IRenderable>(new Text("Unable to load directories"));
        }

        var locationTable = new Table();

        _ = locationTable.AddColumns("Date", "Location").Expand();

        var directoryOffset = displayOptions.SelectedLocation.GetValueOrDefault(state.CursorOffset);

        for (var i = Math.Max(directoryOffset - 3, 0); i < directories.Count; i++)
        {
            var (Location, Date) = directories.ElementAt(i).Key;
            if (i == directoryOffset)
            {
                locationTable.AddRow(
                    new Text(
                        Date.ToShortDateString(),
                        new Style(background: Color.White, foreground: Color.Black)
                    ),
                    new Text(Location, new Style(background: Color.White, foreground: Color.Black))
                );
            }
            else
            {
                locationTable.AddRow(new Text(Date.ToShortDateString()), new Text(Location));
            }
        }

        var sessionTable = new Table().AddColumns("Session").Expand();

        if (displayOptions.SelectedLocation.HasValue)
        {
            var sessions = directories.ElementAt(displayOptions.SelectedLocation.Value).Value;
            for (var i = 0; i < sessions.Count; i++)
            {
                if (i == state.CursorOffset)
                {
                    sessionTable.AddRow(
                        new Text(
                            sessions.ElementAt(i).Session,
                            new Style(background: Color.White, foreground: Color.Black)
                        )
                    );
                }
                else
                {
                    sessionTable.AddRow(new Text(sessions.ElementAt(i).Session));
                }
            }
        }

        var title = $"""
            Select the data directory to run the simulation from. 

            If you cannot see your directory here, ensure that it contains both a file named subscribe.txt and live.txt.
            The directory name must be of the form /<location>_<session-type>/ e.g. /Silverstone_Practice_1/

            To change the data directory, set the OPENF1_DATADIRECTORY environment variable.
            Configured Directory: {options.Value.DataDirectory}
            """;
        var helperText = new Text(title);

        var layout = new Layout("Root").SplitRows(
            new Layout("Title", helperText).Size(8),
            new Layout("Tables").SplitColumns(
                new Layout("Location Table", locationTable),
                new Layout("Session Table", sessionTable)
            )
        );

        return Task.FromResult<IRenderable>(layout);
    }
}
