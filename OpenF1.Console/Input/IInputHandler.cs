namespace OpenF1.Console;

public interface IInputHandler
{
    public bool IsEnabled { get; }

    public Screen[] ApplicableScreens { get; }

    public ConsoleKey[] Keys { get; }

    public string Description { get; }

    /// <summary> 
    /// <list type="bullet">
    /// <item>00-09 = Foundational Input (e.g. Exit/Escape)</item>
    /// <item>20-29 = Common Screen Inputs (e.g cursor, page etc)</item>
    /// <item>40-49 = Primary Screen Inputs (e.g specific actions)</item>
    /// <item>60-69 = Switch Screen Inputs</item>
    /// </list>
    /// </summary>
    public int Sort { get; }

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo);
}
