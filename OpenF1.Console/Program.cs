using System.CommandLine;
using OpenF1.Console;

var rootCommand = new RootCommand("openf1-console");

var isVerboseOption = new Option<bool>(
    ["--verbose", "-v"],
    () => false,
    "Whether verbose logging should be enabled"
);

rootCommand.AddGlobalOption(isVerboseOption);

var isApiEnabledOption = new Option<bool>(
    "--with-api",
    () => false,
    "Whether the API endpoint should be exposed at http://localhost:61937"
);

var dataDirectoryOption = new Option<DirectoryInfo>(
    "--data-directory",
    "The directory to which timing data will be read from and written to"
);

rootCommand.AddOption(isApiEnabledOption);
rootCommand.SetHandler(
    CommandHandler.Root,
    isApiEnabledOption,
    dataDirectoryOption,
    isVerboseOption
);

var importCommand = new Command(
    "import",
    "Import data from the F1 Live Timing API, if you have missed recording a session live."
);

var yearArgument = new Argument<int>("year", "The year the meeting took place.");
var listMeetingKeyOption = new Option<int?>(
    ["--meeting-key", "--meeting"],
    "List sessions inside the specified meeting. If not provided, all meetings in the year will be listed."
);
var importListCommand = new Command(
    "list",
    "List the available Meetings and Sessions that can be imported."
);
importListCommand.AddOption(listMeetingKeyOption);
importListCommand.SetHandler(
    CommandHandler.ListMeetings,
    yearArgument,
    listMeetingKeyOption,
    isVerboseOption
);

importCommand.AddCommand(importListCommand);

var meetingKeyOption = new Option<int>(
    ["--meeting-key", "--meeting"],
    "The Meeting Key of the session to import"
)
{
    IsRequired = true
};

var sessionKeyOption = new Option<int>(
    ["--session-key", "--session"],
    "The Session Key of the session inside the selected meeting to import"
)
{
    IsRequired = true
};
importCommand.AddArgument(yearArgument);
importCommand.AddOption(meetingKeyOption);
importCommand.AddOption(sessionKeyOption);
importCommand.SetHandler(
    CommandHandler.ImportSession,
    yearArgument,
    meetingKeyOption,
    sessionKeyOption,
    dataDirectoryOption,
    isVerboseOption
);

rootCommand.AddCommand(importCommand);

await rootCommand.InvokeAsync(args);
