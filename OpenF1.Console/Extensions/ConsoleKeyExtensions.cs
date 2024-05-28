namespace OpenF1.Console;

public static class ConsoleKeyExtensions
{
    public static string GetConsoleKeyCharacter(this ConsoleKey key) =>
        key switch
        {
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
}
