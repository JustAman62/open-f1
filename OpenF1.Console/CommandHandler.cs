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

        builder
            .Configuration.AddJsonFile(Path.Join(AppContext.BaseDirectory, "appsettings.json"))
            .AddJsonFile(Path.Join(LiveTimingOptions.BaseDirectory, "config.json"), optional: true)
            .AddEnvironmentVariables("OPENF1_")
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(LiveTimingOptions.Verbose)] = isVerbose.ToString(),
                    [nameof(LiveTimingOptions.ApiEnabled)] = isApiEnabled.ToString(),
                    [nameof(LiveTimingOptions.DataDirectory)] = dataDirectory?.FullName
                }
            );

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
                        .AddSimpleConsole(opt =>
                        {
                            opt.SingleLine = true;
                            opt.IncludeScopes = false;
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
