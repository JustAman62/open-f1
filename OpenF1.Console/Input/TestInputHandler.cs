
using Microsoft.Extensions.Options;

namespace OpenF1.Console;

public class TestInputHandler(IOptions<TestDisplayOptions> options) : IInputHandler
{
    public Screen[]? ApplicableScreens => [Screen.Main];

    public ConsoleKey ConsoleKey => ConsoleKey.A;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        options.Value.DisplayedText += "A";
        return Task.CompletedTask;
    }
}
