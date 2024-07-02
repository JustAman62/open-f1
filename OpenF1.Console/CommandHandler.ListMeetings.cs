using OpenF1.Data;
using Spectre.Console;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    public static async Task ListMeetings(int year, int? meetingKey, bool isVerbose)
    {
        var builder = GetBuilder(isVerbose: isVerbose, useConsoleLogging: true);
        var app = builder.Build();

        var importer = app.Services.GetRequiredService<IDataImporter>();
        var res = await importer.GetMeetingsAsync(year);

        if (meetingKey is null)
        {
            AnsiConsole.WriteLine($"Found {res.Meetings.Count} meetings");
            WriteMeetings(res.Meetings);
        }
        else
        {
            var meeting = res.Meetings.SingleOrDefault(x => x.Key == meetingKey);
            if (meeting is null)
            {
                AnsiConsole.Write(
                    new Text(
                        $"Failed to find a meeting with the key {meetingKey}{Environment.NewLine}",
                        Color.Red
                    )
                );
                WriteMeetings(res.Meetings);
                return;
            }

            AnsiConsole.WriteLine(
                $"Found {meeting.Sessions.Count} sessions inside meeting {meetingKey} {meeting.Name}"
            );
            WriteSessions(meeting);
        }
    }

    private static void WriteMeetings(List<ListMeetingsApiResponse.Meeting> meetings)
    {
        var table = new Table().AddColumns(new TableColumn("Key"), new("Meeting Name"));

        table.Title = new TableTitle("Available Meetings");

        foreach (var meeting in meetings)
        {
            table.AddRow(meeting.Key.ToString(), meeting.Name);
        }

        AnsiConsole.Write(table);
    }

    private static void WriteSessions(ListMeetingsApiResponse.Meeting meeting)
    {
        var table = new Table().AddColumns(
            new TableColumn("Key"),
            new("Meeting Name"),
            new("Session Name")
        );

        table.Title = new TableTitle("Available Sessions");

        foreach (var session in meeting.Sessions)
        {
            table.AddRow(session.Key.ToString(), meeting.Name, session.Name);
        }

        AnsiConsole.Write(table);
    }
}
