using InMemLogger;
using OpenF1.Data;
using Serilog;
using Serilog.Events;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    private static WebApplicationBuilder GetBuilder(
        bool isApiEnabled = false,
        DirectoryInfo? dataDirectory = null,
        bool isVerbose = false,
        bool useConsoleLogging = false
    )
    {
        var builder = WebApplication.CreateEmptyBuilder(
            new() { ApplicationName = "openf1-console" }
        );

        var commandLineOpts = new Dictionary<string, string?>();
        if (isVerbose)
        {
            commandLineOpts.Add(nameof(LiveTimingOptions.Verbose), isVerbose.ToString());
        }
        if (isApiEnabled)
        {
            commandLineOpts.Add(nameof(LiveTimingOptions.ApiEnabled), isApiEnabled.ToString());
        }
        if (dataDirectory is not null)
        {
            commandLineOpts.Add(nameof(LiveTimingOptions.DataDirectory), dataDirectory?.FullName);
        }

        builder
            .Configuration.AddJsonFile(
                Path.Join(LiveTimingOptions.BaseDirectory, "config.json"),
                optional: true
            )
            .AddEnvironmentVariables("OPENF1_")
            .AddInMemoryCollection(commandLineOpts);

        var options = builder.Configuration.Get<LiveTimingOptions>() ?? new();

        var (inMemoryLogLevel, fileLogLevel) = options.Verbose
            ? (LogLevel.Trace, LogEventLevel.Verbose)
            : (LogLevel.Information, LogEventLevel.Information);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(fileLogLevel)
            .WriteTo.File(
                path: Path.Join(LiveTimingOptions.BaseDirectory, "logs/openf1-console.log"),
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
            .AddLiveTiming(builder.Configuration);

        builder.WebHost.UseServer(new NullServer());

        return builder;
    }
}
