using Spectre.Console;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    public static async Task ListMeetings(int year, int? meetingKey)
    {
        var httpClient = new HttpClient();
        var url = $"https://livetiming.formula1.com/static/{year}/Index.json";
        var res =
            await httpClient.GetFromJsonAsync<ListMeetingsApiResponse>(url)
            ?? throw new InvalidOperationException("An error occurred parsing the API response");

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
        var table = new Table().AddColumns(
            new TableColumn("Key"),
            new("Meeting Name")
        );

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

    private record ListMeetingsApiResponse
    {
        public required int Year { get; set; }
        public required List<Meeting> Meetings { get; set; }

        public record Meeting
        {
            public required int Key { get; set; }
            public required string Name { get; set; }
            public required string Location { get; set; }
            public required List<Session> Sessions { get; set; }

            public record Session
            {
                public required int Key { get; set; }
                public required string Name { get; set; }
            }
        }
    }
}
