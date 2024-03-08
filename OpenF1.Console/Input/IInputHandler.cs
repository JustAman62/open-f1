namespace OpenF1.Console;

public interface IInputHandler
{
    public Screen[]? ApplicableScreens { get; }

    public ConsoleKey ConsoleKey { get; }

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo);
}
