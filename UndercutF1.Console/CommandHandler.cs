using System.CommandLine;
using InMemLogger;
using Serilog;
using Serilog.Events;
using UndercutF1.Console.ExternalPlayerSync;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task Parse(string[] args, CancellationToken cancellationToken)
    {
        var isVerboseOption = new Option<bool?>("--verbose", "-v")
        {
            Description = "Whether verbose logging should be enabled",
        };
        var isApiEnabledOption = new Option<bool?>("--with-api")
        {
            Description = "Whether the API endpoint should be exposed at http://localhost:61937",
        };
        var dataDirectoryOption = new Option<DirectoryInfo?>("--data-directory")
        {
            Description = "The directory to which timing data will be read from and written to",
        };
        var logDirectoryOption = new Option<DirectoryInfo?>("--log-directory")
        {
            Description = "The directory to which logs will be written to",
        };
        var notifyOption = new Option<bool?>("--notify")
        {
            Description =
                "Whether audible BELs are sent to your terminal when new race control messages are received",
        };
        var preferFfmpegOption = new Option<bool?>("--prefer-ffmpeg")
        {
            Description =
                "Prefer the usage of `ffplay` for playing Team Radio on Mac/Linux, instead of afplay/mpg123. `ffplay` is always used on Windows",
        };
        var preventSleepOption = new Option<bool?>("--prevent-sleep")
        {
            Description =
                "Attempt to prevent the device/display from sleeping whilst the app is running. Intended to help prevent accidental stream disconnection due to inactivity on your device.",
        };
        var forceGraphicsProtocol = new Option<GraphicsProtocol?>("--force-graphics-protocol")
        {
            Description = "Forces the usage of a particular graphics protocol.",
        };

        var rootCommand = new RootCommand("undercutf1")
        {
            isVerboseOption,
            isApiEnabledOption,
            dataDirectoryOption,
            logDirectoryOption,
            notifyOption,
            preferFfmpegOption,
            preventSleepOption,
            forceGraphicsProtocol,
        };

        rootCommand.SetAction(parseResult =>
            Root(
                parseResult.GetValue(isApiEnabledOption),
                parseResult.GetValue(dataDirectoryOption),
                parseResult.GetValue(logDirectoryOption),
                parseResult.GetValue(isVerboseOption),
                parseResult.GetValue(notifyOption),
                parseResult.GetValue(preferFfmpegOption),
                parseResult.GetValue(preventSleepOption),
                parseResult.GetValue(forceGraphicsProtocol)
            )
        );

        var yearArgument = new Argument<int>("year")
        {
            Description = "The year the meeting took place.",
        };
        var meetingKeyOption = new Option<int?>("--meeting-key", "--meeting", "-m")
        {
            Description =
                "The Meeting Key of the session to import. If not provided, all meetings in the year will be listed.",
        };
        var sessionKeyOption = new Option<int?>("--session-key", "--session", "-s")
        {
            Description =
                "The Session Key of the session inside the selected meeting to import. If not provided, all sessions in the provided meeting will be listed.",
        };

        var importCommand = new Command(
            "import",
            """
            Imports data from the F1 Live Timing API, if you have missed recording a session live. 
            The data is imported in a format that can be replayed in real-time using undercutf1.
            """
        )
        {
            yearArgument,
            meetingKeyOption,
            sessionKeyOption,
            dataDirectoryOption,
            logDirectoryOption,
            isVerboseOption,
        };

        importCommand.SetAction(res =>
            ImportSession(
                res.GetValue(yearArgument),
                res.GetValue(meetingKeyOption),
                res.GetValue(sessionKeyOption),
                res.GetValue(dataDirectoryOption),
                res.GetValue(logDirectoryOption),
                res.GetValue(isVerboseOption)
            )
        );

        rootCommand.Subcommands.Add(importCommand);

        var infoCommand = new Command(
            "info",
            """
            Prints diagnostics about undercutf1, and the terminal in the command is run in.
            """
        )
        {
            dataDirectoryOption,
            logDirectoryOption,
            isVerboseOption,
            notifyOption,
            preferFfmpegOption,
            preventSleepOption,
            forceGraphicsProtocol,
        };
        infoCommand.SetAction(res =>
            GetInfo(
                res.GetValue(dataDirectoryOption),
                res.GetValue(logDirectoryOption),
                res.GetValue(isVerboseOption),
                res.GetValue(notifyOption),
                res.GetValue(preferFfmpegOption),
                res.GetValue(preventSleepOption),
                res.GetValue(forceGraphicsProtocol)
            )
        );
        rootCommand.Subcommands.Add(infoCommand);

        var graphicsProtocolArgument = new Argument<GraphicsProtocol>(
            "The graphics protocol to use"
        );
        var imageFilePathArgument = new Argument<FileInfo>("file");

        var imageCommand = new Command(
            "image",
            """
            Displays the image from the provided filepath in the terminal, using the appropiate graphics protocol.
            """
        )
        {
            imageFilePathArgument,
            graphicsProtocolArgument,
            isVerboseOption,
        };
        imageCommand.SetAction(res =>
            OutputImage(
                res.GetRequiredValue(imageFilePathArgument),
                res.GetValue(graphicsProtocolArgument),
                res.GetValue(isVerboseOption)
            )
        );
        rootCommand.Subcommands.Add(imageCommand);

        var loginCommand = new Command(
            "login",
            """
            Login to your Formula 1 account to unlock all data feeds (like the driver tracker).
            """
        )
        {
            isVerboseOption,
        };
        loginCommand.SetAction(res => LoginToFormula1Account(res.GetValue(isVerboseOption)));
        rootCommand.Subcommands.Add(loginCommand);

        var logoutCommand = new Command(
            "logout",
            """
            Logout of your Formula 1 account.
            """
        )
        {
            isVerboseOption,
        };
        logoutCommand.SetAction(res => LogoutOfFormula1Account());
        rootCommand.Subcommands.Add(logoutCommand);

        await rootCommand.Parse(args).InvokeAsync(new(), cancellationToken);
    }

    private static WebApplicationBuilder GetBuilder(
        bool? isApiEnabled = false,
        DirectoryInfo? dataDirectory = null,
        DirectoryInfo? logDirectory = null,
        bool? isVerbose = false,
        bool? notifyEnabled = null,
        bool? preferFfmpeg = null,
        bool? preventDisplaySleep = null,
        GraphicsProtocol? forceGraphicsProtocol = null,
        bool useConsoleLogging = false
    )
    {
        var builder = WebApplication.CreateEmptyBuilder(new() { ApplicationName = "undercutf1" });

        var commandLineOpts = new Dictionary<string, string?>();
        if (isVerbose is not null)
        {
            commandLineOpts.Add(nameof(ConsoleOptions.Verbose), isVerbose.ToString());
        }
        if (isApiEnabled is not null)
        {
            commandLineOpts.Add(nameof(ConsoleOptions.ApiEnabled), isApiEnabled.ToString());
        }
        if (dataDirectory is not null)
        {
            commandLineOpts.Add(nameof(ConsoleOptions.DataDirectory), dataDirectory?.FullName);
        }
        if (logDirectory is not null)
        {
            commandLineOpts.Add(nameof(ConsoleOptions.LogDirectory), logDirectory?.FullName);
        }
        if (notifyEnabled is not null)
        {
            commandLineOpts.Add(nameof(ConsoleOptions.Notify), notifyEnabled.ToString());
        }
        if (preferFfmpeg is not null)
        {
            commandLineOpts.Add(
                nameof(ConsoleOptions.PreferFfmpegPlayback),
                preferFfmpeg.ToString()
            );
        }
        if (preventDisplaySleep is not null)
        {
            commandLineOpts.Add(
                nameof(ConsoleOptions.PreventDisplaySleep),
                preventDisplaySleep.ToString()
            );
        }
        if (forceGraphicsProtocol is not null)
        {
            commandLineOpts.Add(
                nameof(ConsoleOptions.ForceGraphicsProtocol),
                forceGraphicsProtocol.ToString()
            );
        }

        _ = builder
            .Configuration.AddJsonFile(
                ConsoleOptions.ConfigFilePath,
                optional: true,
                reloadOnChange: true
            )
            .AddEnvironmentVariables("UNDERCUTF1_")
            .AddInMemoryCollection(commandLineOpts);

        var options = builder.Configuration.Get<ConsoleOptions>() ?? new();

        var (inMemoryLogLevel, fileLogLevel) = options.Verbose
            ? (LogLevel.Trace, LogEventLevel.Verbose)
            : (LogLevel.Information, LogEventLevel.Information);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(fileLogLevel)
            .WriteTo.File(
                path: Path.Join(options.LogDirectory, "/undercutf1.log"),
                rollOnFileSizeLimit: true,
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 5
            )
            .CreateLogger();

        builder
            .Services.AddOptions()
            .AddLogging(configure =>
            {
                if (useConsoleLogging)
                {
                    configure
                        .ClearProviders()
                        .SetMinimumLevel(inMemoryLogLevel)
                        .AddSerilog()
                        .AddTerminal(opt =>
                        {
                            opt.SingleLine = true;
                            opt.UseColors = true;
                            opt.UseUtcTimestamp = true;
                        });
                }
                else
                {
                    configure
                        .ClearProviders()
                        .SetMinimumLevel(inMemoryLogLevel)
                        .AddInMemory()
                        .AddSerilog();
                }
            })
            .Configure<ConsoleOptions>(builder.Configuration)
            .AddLiveTiming(builder.Configuration)
            .AddSingleton<WebSocketSynchroniser>()
            .AddSingleton<AccountLogin>();

        builder.WebHost.UseServer(new NullServer());

        return builder;
    }

    private static async Task EnsureConfigFileExistsAsync(ILogger logger)
    {
        try
        {
            if (File.Exists(ConsoleOptions.ConfigFilePath))
            {
                return;
            }
            var schemaLocation =
                "https://raw.githubusercontent.com/JustAman62/undercut-f1/refs/heads/master/config.schema.json";
            var defaultConfigFile = $$"""
                {
                    "$schema": "{{schemaLocation}}"
                }
                """;

            logger.LogInformation(
                "Writing default configuration file to {Path}",
                ConsoleOptions.ConfigFilePath
            );
            Directory.CreateDirectory(Directory.GetParent(ConsoleOptions.ConfigFilePath)!.FullName);
            await File.WriteAllTextAsync(ConsoleOptions.ConfigFilePath, defaultConfigFile);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to write default configuration file to {Path}",
                ConsoleOptions.ConfigFilePath
            );
        }
    }
}
