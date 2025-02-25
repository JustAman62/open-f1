using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenF1.Console;

public sealed partial class TerminalInfoProvider
{
    private readonly ILogger<TerminalInfoProvider> _logger;

    private static readonly string[] ITERM_PROTOCOL_SUPPORTED_TERMINALS = ["iterm", "wezterm"];

    [GeneratedRegex(@"\u001B_Gi=31;(.+)\u001B\\")]
    private static partial Regex TerminalKittyGraphicsResponseRegex();

    [GeneratedRegex(@"\u001B\[\?2026;[123]\$y")]
    private static partial Regex TerminalSynchronizedOutputResponseRegex();

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.
    /// This is done in a very rudimentary way, and is by no means comprehensive.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports the iTerm 2 Graphics Protocol.</returns>
    public Lazy<bool> IsITerm2ProtocolSupported { get; }

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal supports the Kitty Graphics Protocol.
    /// This is done by sending an escape code to the terminal which supported terminals should respond to.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports the Kitty Graphics Protocol.</returns>
    public Lazy<bool> IsKittyProtocolSupported { get; }

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal support Synchronized Output,
    /// as described in https://gist.github.com/christianparpart/d8a62cc1ab659194337d73e399004036
    /// and https://gitlab.com/gnachman/iterm2/-/wikis/synchronized-updates-spec.
    /// </summary>
    public Lazy<bool> IsSynchronizedOutputSupported { get; }

    public TerminalInfoProvider(ILogger<TerminalInfoProvider> logger)
    {
        _logger = logger;
        IsITerm2ProtocolSupported = new Lazy<bool>(GetIsITerm2ProtocolSupported);
        IsKittyProtocolSupported = new Lazy<bool>(GetKittyProtocolSupported);
        IsSynchronizedOutputSupported = new Lazy<bool>(GetSynchronizedOutputSupported);
    }

    private bool GetIsITerm2ProtocolSupported()
    {
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? string.Empty;
        var supported = ITERM_PROTOCOL_SUPPORTED_TERMINALS.Any(x =>
            termProgram.Contains(x, StringComparison.InvariantCultureIgnoreCase)
        );
        _logger.LogDebug("iTerm2 Graphics Protocol Supported: {Supported}", supported);
        return supported;
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

            var supported =
                match.Success
                && match.Groups.Count == 2
                && match
                    .Groups[1]
                    .Captures[0]
                    .Value.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
            _logger.LogDebug(
                "Kitty Protocol Supported: {Supported}, Response: {Response}",
                supported,
                SanitizeResponse(str)
            );
            return supported;
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

    private bool GetSynchronizedOutputSupported()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16);
        try
        {
            // Send a DECRQM query to the terminal to check for support
            Terminal.Out("\u001B[?2026$p");
            // Also send a device attributes escape code, so that there is always something to read from stdin
            Terminal.Out("\u001B[c");

            // Expected response: <ESC> [ ? 2026 ; 0 $ y
            Terminal.Read(buffer);
            var str = Encoding.ASCII.GetString(buffer);
            var match = TerminalSynchronizedOutputResponseRegex().Match(str);

            _logger.LogDebug(
                "Synchronized Output Supported: {Supported}, Response: {Response}",
                match.Success,
                SanitizeResponse(str)
            );

            return match.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to determine if terminal supports Synchronized Output");
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private string SanitizeResponse(string str) => str.Replace("\u001B", "<ESC>");
}
