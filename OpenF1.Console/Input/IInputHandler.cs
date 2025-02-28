namespace OpenF1.Console;

public interface IInputHandler
{
    bool IsEnabled { get; }

    Screen[] ApplicableScreens { get; }

    /// <summary>
    /// Which keys will activate this input handler.
    /// </summary>
    ConsoleKey[] Keys { get; }

    /// <summary>
    /// The keys to display to the user that would activate this input handler.
    // Defaults to <see cref="Keys"/>. Use this to have keys which will activate
    // this handler without showing the user that those work will work. 
    // For example, for alternate bindings (e.g. HJKL instead of arrow keys).
    /// </summary>
    ConsoleKey[] DisplayKeys => Keys;

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
