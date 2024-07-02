using OpenF1.Data;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    public static async Task ImportSession(
        int year,
        int meetingKey,
        int sessionKey,
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
        await importer.ImportSessionAsync(year, meetingKey, sessionKey);
    }
}
