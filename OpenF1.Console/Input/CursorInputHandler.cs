namespace OpenF1.Console;

public sealed class CursorInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey[] Keys =>
        [ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.J, ConsoleKey.K];

    public ConsoleKey[] DisplayKeys => [ConsoleKey.UpArrow, ConsoleKey.DownArrow];

    public string Description => $"Cursor {state.CursorOffset}";

    public int Sort => 21;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var changeBy = consoleKeyInfo.Key is ConsoleKey.DownArrow or ConsoleKey.J ? 1 : -1;
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
