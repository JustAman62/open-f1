namespace OpenF1.Console;

public static class ConsoleKeyExtensions
{
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
            _ => key.ToString()
        };

    public static string ToDisplayCharacters(this ConsoleKey[] keys)
    {
        var characters = keys.Where(x => x != (ConsoleKey)3) // Remove the ^C key code
            .Select(x => x.GetConsoleKeyDisplayCharacter());
        return string.Join('/', characters);
    }
}
