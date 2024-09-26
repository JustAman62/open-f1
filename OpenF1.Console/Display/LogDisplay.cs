using InMemLogger;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public record LogDisplayOptions
{
    public LogLevel MinimumLogLevel = LogLevel.Information;
}

public class LogDisplay(State state, InMemoryLogger inMemoryLogger, LogDisplayOptions options) : IDisplay
{
    public Screen Screen => Screen.Logs;

    public Task<IRenderable> GetContentAsync()
    {
        var logs = inMemoryLogger
            .RecordedLogs.Where(x => x.Level >= options.MinimumLogLevel)
            .Reverse()
            .Select(x => $"{x.Level} {x.Message} {x.Exception}")
            .Skip(state.CursorOffset)
            .Take(5);

        var rowTexts = new List<IRenderable>()
        {
            new Text($"Minimum Log Level: {options.MinimumLogLevel}")
        };
        rowTexts.AddRange(logs.Select(x => new Text(x)));
        var rows = new Rows(rowTexts);
        return Task.FromResult<IRenderable>(new Panel(rows).Expand());
    }
}
