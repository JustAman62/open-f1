namespace OpenF1.Console;

public static class ConsoleKeyExtensions
{
    private static readonly ConsoleKey[] _hiddenKeys =
    [
        (ConsoleKey)3, // ^C
    ];

    public static string GetConsoleKeyDisplayCharacter(this ConsoleKey key) =>
        key switch
        {
            (ConsoleKey)3 => "^C",
            ConsoleKey.Escape => "Esc",
            ConsoleKey.UpArrow => "▲",
            ConsoleKey.DownArrow => "▼",
            ConsoleKey.LeftArrow => "◄",
            ConsoleKey.RightArrow => "►",
            ConsoleKey.OemComma => ",",
            ConsoleKey.OemPeriod => ".",
            ConsoleKey.Enter => "⏎",
            _ => key.ToString(),
        };

    public static string ToDisplayCharacters(this ConsoleKey[] keys)
    {
        var characters = keys.Except(_hiddenKeys).Select(GetConsoleKeyDisplayCharacter);
        return string.Join('/', characters);
    }
}
