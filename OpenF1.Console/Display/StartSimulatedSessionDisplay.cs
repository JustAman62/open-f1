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

        var tables = new List<Table>();

        var locationTable = new Table()
            .AddColumns(
                new TableColumn("Date ").Width(11).RightAligned(),
                new TableColumn("Location").LeftAligned()
            )
            .NoBorder()
            .NoSafeBorder()
            .RemoveColumnPadding()
            .Expand();

        tables.Add(locationTable);

        var selected = displayOptions.SelectedLocation.GetValueOrDefault(state.CursorOffset);
        var maxLocationLength = directories.Select(x => x.Key.Location.Length).Max();

        for (var i = Math.Max(selected - 3, 0); i < directories.Count; i++)
        {
            var (Location, Date) = directories.ElementAt(i).Key;
            if (i == selected)
            {
                locationTable.AddRow(
                    new Text(Date.ToShortDateString() + " ", DisplayUtils.STYLE_INVERT),
                    new Text(
                        Location.ToFixedWidth(maxLocationLength, padRight: true),
                        DisplayUtils.STYLE_INVERT
                    )
                );
            }
            else
            {
                locationTable.AddRow(new Text(Date.ToShortDateString() + " "), new Text(Location));
            }
        }

        var sessionTable = new Table()
            .AddColumns(new TableColumn("Session"), new TableColumn(" Directory"))
            .NoBorder()
            .NoSafeBorder()
            .RemoveColumnPadding()
            .Expand();

        if (displayOptions.SelectedLocation.HasValue)
        {
            var sessions = directories.ElementAt(displayOptions.SelectedLocation.Value).Value;

            // Figure out the space available for the Directory column
            const int dateColumnLength = 11;
            var maxSessionTypeLength = sessions.Select(x => x.Session.Length).Max();

            // Gives space taken up by everything except the directory column
            var directoryAvailableWidth =
                Terminal.Size.Width
                - dateColumnLength
                - maxLocationLength
                - maxSessionTypeLength
                - 4;

            for (var i = 0; i < sessions.Count; i++)
            {
                if (i == state.CursorOffset)
                {
                    sessionTable.AddRow(
                        new Text(
                            sessions
                                .ElementAt(i)
                                .Session.ToFixedWidth(maxSessionTypeLength + 1, padRight: true),
                            DisplayUtils.STYLE_INVERT
                        ),
                        new Text(
                            sessions
                                .ElementAt(i)
                                .Directory.ToFixedWidth(directoryAvailableWidth, padRight: true),
                            DisplayUtils.STYLE_INVERT
                        )
                    );
                }
                else
                {
                    sessionTable.AddRow(
                        new Text(
                            sessions
                                .ElementAt(i)
                                .Session.ToFixedWidth(maxSessionTypeLength + 1, padRight: true)
                        ),
                        new Text(
                            sessions
                                .ElementAt(i)
                                .Directory.ToFixedWidth(directoryAvailableWidth, padRight: true)
                        )
                    );
                }
            }
            tables.Add(sessionTable);
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
            new Layout("Tables", new Columns(tables).Collapse())
        );

        return Task.FromResult<IRenderable>(layout);
    }
}
