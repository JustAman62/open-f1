using System.Security.Cryptography;
using InMemLogger;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public record LogDisplayOptions
{
    public LogLevel MinimumLogLevel = LogLevel.Information;
}

public class LogDisplay(InMemoryLogger inMemoryLogger, LogDisplayOptions options) : IDisplay
{
    public Screen Screen => Screen.Logs;

    public Task<IRenderable> GetContentAsync()
    {
        var logs = inMemoryLogger
            .RecordedLogs.Where(x => x.Level > options.MinimumLogLevel)
            .Select(x => $"{x.Level} {x.Message} {x.Exception}")
            .Take(AnsiConsole.Profile.Height / 2);

        var rowTexts = new List<IRenderable>()
        {
            new Text($"Minimum Log Level: {options.MinimumLogLevel}")
        };
        rowTexts.AddRange(logs.Select(x => new Text(x)));
        var rows = new Rows(rowTexts);
        return Task.FromResult<IRenderable>(new Panel(rows).Expand());
    }

    public class LogDisplayInputHandler(LogDisplayOptions options) : IInputHandler
    {
        public Screen[]? ApplicableScreens => [Screen.Logs];

        public ConsoleKey ConsoleKey => ConsoleKey.M;

        public string Description => "Change Minimum Log Level";

        public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
        {
            options.MinimumLogLevel = options.MinimumLogLevel switch
            {
                LogLevel.Information => LogLevel.Warning,
                LogLevel.Warning => LogLevel.Error,
                _ => LogLevel.Information
            };

            return Task.CompletedTask;
        }
    }
}
