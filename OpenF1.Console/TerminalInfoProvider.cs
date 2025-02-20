using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenF1.Console;

public sealed partial class TerminalInfoProvider
{
    private readonly ILogger<TerminalInfoProvider> _logger;

    private static readonly string[] ITERM_PROTOCOL_SUPPORTED_TERMINALS = ["iterm2"];

    [GeneratedRegex(@"\u001B_Gi=31;(.+)\u001B\\")]
    private static partial Regex TerminalKittyGraphicsResponseRegex();

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.
    /// This is done in a very rudimentary way, and is by no means comprehensive.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.</returns>
    public Lazy<bool> IsITerm2ProtocolSupported { get; } =
        new Lazy<bool>(GetIsITerm2ProtocolSupported);

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal supports the Kitty Graphics Protocol.
    /// This is done by sending an escape code to the terminal which supported terminals should respond to.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports the Kitty Graphics Protocol.</returns>
    public Lazy<bool> IsKittyProtocolSupported { get; }

    public TerminalInfoProvider(ILogger<TerminalInfoProvider> logger)
    {
        _logger = logger;
        IsITerm2ProtocolSupported = new Lazy<bool>(GetIsITerm2ProtocolSupported);
        IsKittyProtocolSupported = new Lazy<bool>(GetKittyProtocolSupported);
    }

    private static bool GetIsITerm2ProtocolSupported()
    {
        var lcTerminal = Environment.GetEnvironmentVariable("LC_TERMINAL") ?? string.Empty;
        return ITERM_PROTOCOL_SUPPORTED_TERMINALS.Contains(
            lcTerminal,
            StringComparer.InvariantCultureIgnoreCase
        );
    }

    private bool GetKittyProtocolSupported()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16);
        try
        {
            // Query the terminal with a graphic protocol specific escape code
            Terminal.Out("\u001B_Gi=31,s=1,v=1,a=q,t=d,f=24;AAAA\u001B\\");
            // Also send a device attributes escape code, so that there is always something to read from stdin
            Terminal.Out("\u001B[c");

            // Expected response: <ESC>_Gi=31;error message or OK<ESC>\
            Terminal.Read(buffer);
            var str = Encoding.ASCII.GetString(buffer);
            var match = TerminalKittyGraphicsResponseRegex().Match(str);

            return match.Success
                && match.Groups.Count == 2
                && match
                    .Groups[1]
                    .Captures[0]
                    .Value.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to determine if terminal supports Kitty Graphics Protocol");
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
