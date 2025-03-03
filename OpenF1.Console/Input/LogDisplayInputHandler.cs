namespace OpenF1.Console;

public class LogDisplayInputHandler(LogDisplayOptions options) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Logs];

    public ConsoleKey[] Keys => [ConsoleKey.M];

    public string Description => $"Log Level: {options.MinimumLogLevel}";

    public int Sort => 20;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        options.MinimumLogLevel = options.MinimumLogLevel switch
        {
            LogLevel.Debug => LogLevel.Information,
            LogLevel.Information => LogLevel.Warning,
            LogLevel.Warning => LogLevel.Error,
            _ => LogLevel.Debug,
        };

        return Task.CompletedTask;
    }
}
