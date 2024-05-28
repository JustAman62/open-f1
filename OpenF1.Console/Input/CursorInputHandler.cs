namespace OpenF1.Console;

public sealed class CursorInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey[] Keys => [ConsoleKey.UpArrow, ConsoleKey.DownArrow];

    public string Description => $"Cursor {state.CursorOffset}";

    public int Sort => 21;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var changeBy = consoleKeyInfo.Key == ConsoleKey.DownArrow ? 1 : -1;
        if (consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            changeBy *= 5;
        }

        state.CursorOffset += changeBy;

        if (state.CursorOffset < 0)
        {
            state.CursorOffset = 0;
        }

        return Task.CompletedTask;
    }
}
