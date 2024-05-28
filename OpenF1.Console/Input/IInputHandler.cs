namespace OpenF1.Console;

public interface IInputHandler
{
    public bool IsEnabled { get; }

    public Screen[] ApplicableScreens { get; }

    public ConsoleKey[] Keys { get; }

    public string Description { get; }

    public int Sort { get; }

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo);
}
