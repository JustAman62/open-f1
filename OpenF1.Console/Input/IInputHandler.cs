namespace OpenF1.Console;

public interface IInputHandler
{
    bool IsEnabled { get; }

    Screen[] ApplicableScreens { get; }

    ConsoleKey[] Keys { get; }

    string Description { get; }

    /// <summary>
    /// <list type="bullet">
    /// <item>00-09 = Foundational Input (e.g. Exit/Escape)</item>
    /// <item>20-29 = Common Screen Inputs (e.g cursor, page etc)</item>
    /// <item>40-49 = Primary Screen Inputs (e.g specific actions)</item>
    /// <item>60-69 = Switch Screen Inputs</item>
    /// </list>
    /// </summary>
    int Sort { get; }

    Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo, CancellationToken cancellationToken = default);
}
