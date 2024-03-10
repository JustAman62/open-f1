using Microsoft.Extensions.Logging;

namespace OpenF1.Console;

public class LogDisplayInputHandler(LogDisplayOptions options) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.Logs];

    public ConsoleKey ConsoleKey => ConsoleKey.M;

    public string Description => "Change Minimum Log Level";

    public int Sort => 1;

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
