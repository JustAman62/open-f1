namespace OpenF1.Console;

public sealed class CursorUpInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => state.CursorOffset != 0;

    public Screen[] ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey ConsoleKey => ConsoleKey.UpArrow;

    public string Description => "Up";

    public int Sort => 11;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        if (consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            state.CursorOffset -= 5;
        }
        else
        {
            state.CursorOffset--;
        }

        if (state.CursorOffset < 0)
        {
            state.CursorOffset = 0;
        }

        return Task.CompletedTask;
    }
}

public sealed class CursorDownInputHandler(State state) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        Enum.GetValues<Screen>().Where(x => x != Screen.Main).ToArray();

    public ConsoleKey ConsoleKey => ConsoleKey.DownArrow;

    public string Description => $"Down ({state.CursorOffset})";

    public int Sort => 10;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        if (consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            state.CursorOffset += 5;
        }
        else
        {
            state.CursorOffset++;
        }
        return Task.CompletedTask;
    }
}
