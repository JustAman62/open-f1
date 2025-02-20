namespace OpenF1.Console;

internal static class StringExtensions
{
    public static string ToFixedWidth(this string str, int width, bool padRight = false) =>
        padRight ? str.PadRight(width)[..width] : str.PadLeft(width)[..width];
}
