using OpenF1.Data;
using Spectre.Console;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    public static async Task ImportSession(
        int year,
        int? meetingKey,
        int? sessionKey,
        DirectoryInfo? dataDirectory,
        bool isVerbose
    )
    {
        var builder = GetBuilder(
            dataDirectory: dataDirectory,
            isVerbose: isVerbose,
            useConsoleLogging: true
        );
        var app = builder.Build();
        var importer = app.Services.GetRequiredService<IDataImporter>();

        if (!meetingKey.HasValue)
        {
            var res = await importer.GetMeetingsAsync(year);
            WriteMeetings(res.Meetings);
        }
        else if (!sessionKey.HasValue)
        {
            var res = await importer.GetMeetingsAsync(year);
            var meeting = res.Meetings.SingleOrDefault(x => x.Key == meetingKey);
            if (meeting is null)
            {
                AnsiConsole.Write(
                    new Text(
                        $"Failed to find a meeting with the provided key {meetingKey}{Environment.NewLine}",
                        Color.Red
                    )
                );
                WriteMeetings(res.Meetings);
                return;
            }

            await Terminal.OutLineAsync(
                $"Found {meeting.Sessions.Count} sessions inside meeting {meetingKey} {meeting.Name}"
            );
            WriteSessions(meeting);
        }
        else
        {
            await importer.ImportSessionAsync(year, meetingKey.Value, sessionKey.Value);
        }
    }
}
