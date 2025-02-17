namespace OpenF1.Console;

public static class TerminalGraphics
{
    private const string ESCAPE_OSC = "\e]"; // Begins an Operating System Command
    private const string ESCAPE_ST = "\e\\"; // String Terminator

    private static readonly string[] ITERM_PROTOCOL_SUPPORTED_TERMINALS = ["iterm2"];

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.
    /// This is done in a very rudimentary way, and is by no means comprehensive.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.</returns>
    public static bool IsITerm2ProtocolSupported()
    {
        var lcTerminal = Environment.GetEnvironmentVariable("LC_TERMINAL") ?? string.Empty;
        return ITERM_PROTOCOL_SUPPORTED_TERMINALS.Contains(
            lcTerminal,
            StringComparer.InvariantCultureIgnoreCase
        );
    }

    /// <summary>
    /// Given a base64 encoded string of the PNG file, 
    /// returns the control sequence for displaying that image in the terminal.
    /// </summary>
    /// <returns>The control sequence as a string.</returns>
    public static string ITerm2GraphicsSequence(int height, int width, string base64EncodedImage)
    {
        var args = new string[] {
            "name=drivertracker",
            $"width={width}",
            $"height={height}",
            $"preserveAspectRatio=1",
            "inline=1"
        };

        return $"{ESCAPE_OSC}1337;File={string.Join(';', args)}:{base64EncodedImage}{ESCAPE_ST}";
    }
}
