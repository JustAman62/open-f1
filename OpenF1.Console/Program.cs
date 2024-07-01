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

var dataDirectoryOption = new Option<string>(
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

var listYearOption = new Option<int>(
    "--year",
    "Lists all meetings that took place in the provided year."
)
{
    IsRequired = true
};
var listMeetingKeyOption = new Option<int?>(
    "--meeting-key",
    "List sessions inside the specified meeting. If not provided, all meetings in the year will be listed."
);
var importListCommand = new Command(
    "list",
    "List the available Meetings and Sessions that can be imported."
);
importListCommand.AddOption(listYearOption);
importListCommand.AddOption(listMeetingKeyOption);
importListCommand.SetHandler(CommandHandler.ListMeetings, listYearOption, listMeetingKeyOption);

importCommand.AddCommand(importListCommand);

rootCommand.AddCommand(importCommand);

await rootCommand.InvokeAsync(args);
