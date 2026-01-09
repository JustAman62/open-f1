using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using UndercutF1.Console.Graphics;

namespace UndercutF1.Console;

public sealed partial class TerminalInfoProvider
{
    private readonly IOptions<ConsoleOptions> _options;
    private readonly ILogger<TerminalInfoProvider> _logger;

    private static readonly string[] ITERM_PROTOCOL_SUPPORTED_TERMINALS = ["iterm", "wezterm"];

    [GeneratedRegex(@"\e_G(.+)\e\\")]
    private static partial Regex TerminalKittyGraphicsResponseRegex();

    [GeneratedRegex(@"\e\[\?2026;(\d+)\$y")]
    private static partial Regex TerminalSynchronizedOutputResponseRegex();

    [GeneratedRegex(@"\e\[4;(\d+);(\d+)t")]
    private static partial Regex TerminalSizeResponseRegex();

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
    /// Returns <see langword="true"/> if the current terminal supports Sixel graphics.
    /// This is done by checking the terminals response to a primary device attributes escape code.
    /// </summary>
    /// <returns><see langword="true" /> if the current terminal supports Sixel graphics.</returns>
    public Lazy<bool> IsSixelSupported { get; }

    /// <summary>
    /// Returns <see langword="true" /> if the current terminal support Synchronized Output,
    /// as described in https://gist.github.com/christianparpart/d8a62cc1ab659194337d73e399004036
    /// and https://gitlab.com/gnachman/iterm2/-/wikis/synchronized-updates-spec.
    /// </summary>
    public Lazy<bool> IsSynchronizedOutputSupported { get; }

    /// <summary>
    /// Returns the size of the terminal in pizels. <c>null</c> if terminal size could not be determined.
    /// If the terminal is likely returning raw pizels instead of points, HiDpi will be set to true.
    /// </summary>
    public Lazy<TerminalSizeInfo> TerminalSize { get; private set; }

    public TerminalInfoProvider(
        IOptions<ConsoleOptions> options,
        ILogger<TerminalInfoProvider> logger
    )
    {
        _options = options;
        _logger = logger;
        IsITerm2ProtocolSupported = new Lazy<bool>(GetIsITerm2ProtocolSupported);
        IsKittyProtocolSupported = new Lazy<bool>(GetKittyProtocolSupported);
        IsSixelSupported = new Lazy<bool>(GetSixelSupported);
        IsSynchronizedOutputSupported = new Lazy<bool>(GetSynchronizedOutputSupported);
        TerminalSize = new Lazy<TerminalSizeInfo>(GetTerminalSize);
        Terminal.Resized += (_new) =>
        {
            TerminalSize = new Lazy<TerminalSizeInfo>(GetTerminalSize);
            _ = TerminalSize.Value;
        };
    }

    private bool GetIsITerm2ProtocolSupported()
    {
        // Override support response if config is set
        if (_options.Value.ForceGraphicsProtocol.HasValue)
        {
            return _options.Value.ForceGraphicsProtocol.Value == GraphicsProtocol.iTerm;
        }

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? string.Empty;
        var supported = ITERM_PROTOCOL_SUPPORTED_TERMINALS.Any(x =>
            termProgram.Contains(x, StringComparison.InvariantCultureIgnoreCase)
        );
        _logger.LogDebug("iTerm2 Graphics Protocol Supported: {Supported}", supported);
        return supported;
    }

    private bool GetKittyProtocolSupported()
    {
        // Override support response if config is set
        if (_options.Value.ForceGraphicsProtocol.HasValue)
        {
            return _options.Value.ForceGraphicsProtocol.Value == GraphicsProtocol.Kitty;
        }

        PrepareTerminal();
        var buffer = RentBuffer();
        try
        {
            // Query the terminal with a graphic protocol specific escape code
            Terminal.Out("\e_Gi=31,s=1,v=1,a=q,t=d,f=24;AAAA\e\\");
            // Also send a device attributes escape code, so that there is always something to read from stdin
            Terminal.Out("\e[c");

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
                    .Value.Equals("i=31;OK", StringComparison.InvariantCultureIgnoreCase);
            _logger.LogDebug(
                "Kitty Protocol Supported: {Supported}, Response: {Response}",
                supported,
                Util.Sanitize(str)
            );

            if (match.Success && !str.Contains("\e[?"))
            {
                DiscardExtraResponse();
            }
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
        var buffer = RentBuffer();
        try
        {
            // Send a DECRQM query to the terminal to check for support
            Terminal.Out("\e[?2026$p");
            // Also send a device attributes escape code, so that there is always something to read from stdin
            Terminal.Out("\e[c");

            // Expected response: <ESC> [ ? 2026 ; 1 $ y
            Terminal.Read(buffer);
            var str = Encoding.ASCII.GetString(buffer);
            var match = TerminalSynchronizedOutputResponseRegex().Match(str);

            var supported = false;
            if (
                match.Success
                && match.Groups.Count == 2
                && int.TryParse(match.Groups[1].Captures[0].Value, out var responseValue)
            )
            {
                // See https://gist.github.com/christianparpart/d8a62cc1ab659194337d73e399004036#feature-detection
                supported = responseValue > 0 && responseValue < 4;
            }

            _logger.LogDebug(
                "Synchronized Output Supported: {Supported}, Response: {Response}",
                supported,
                Util.Sanitize(str)
            );

            // DECRQM response with \e[? as well, so ignore first 3 chars then check for \e[? again to see if we've
            // already read the device attribute query
            if (match.Success && !str[3..].Contains("\e[?"))
            {
                DiscardExtraResponse();
            }

            return supported;
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

    private TerminalSizeInfo GetTerminalSize()
    {
        PrepareTerminal();
        var buffer = RentBuffer();
        try
        {
            // Send a DCS query to the terminal to query terminal size
            Terminal.Out("\e[14t");
            // Also send a device attributes escape code, so that there is always something to read from stdin
            Terminal.Out("\e[c");

            // Expected response: <ESC> [ ; <height> ; <width> t
            Terminal.Read(buffer);
            var str = Encoding.ASCII.GetString(buffer);
            var match = TerminalSizeResponseRegex().Match(str);

            var height = 0;
            var width = 0;
            _ =
                match.Success
                && match.Groups.Count == 3
                && int.TryParse(match.Groups[1].Captures[0].Value, out height)
                && int.TryParse(match.Groups[2].Captures[0].Value, out width);

            var terminalSizeInfo = new TerminalSizeInfo(
                height,
                width,
                Terminal.Size.Height,
                Terminal.Size.Width,
                height / Terminal.Size.Height,
                width / Terminal.Size.Width
            );

            // Fudge for when iTerm because it reports it's resolution in scaled points instead of raw pixels, but when
            // it draws Sixels it draws raw pixels. So we double the reported terminal size to try and account for that
            // It's quite hacky
            if (Environment.GetEnvironmentVariable("TERM_PROGRAM") == "iTerm.app")
            {
                terminalSizeInfo = terminalSizeInfo with
                {
                    Height = terminalSizeInfo.Height * 2,
                    Width = terminalSizeInfo.Width * 2,
                    PixelsPerRow = terminalSizeInfo.PixelsPerRow * 2,
                    PixelsPerColumn = terminalSizeInfo.PixelsPerColumn * 2,
                };
            }

            _logger.LogDebug(
                "Terminal Size: {TerminalSizeInfo}: {Response}",
                terminalSizeInfo,
                Util.Sanitize(str)
            );

            if (!str.Contains("\e[?"))
            {
                DiscardExtraResponse();
            }

            return terminalSizeInfo;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch terminal size in pixels");
            return new(1, 1, 1, 1, 1, 1);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool GetSixelSupported()
    {
        // Override support response if config is set
        if (_options.Value.ForceGraphicsProtocol.HasValue)
        {
            return _options.Value.ForceGraphicsProtocol.Value == GraphicsProtocol.Sixel;
        }

        PrepareTerminal();
        var buffer = RentBuffer();
        try
        {
            // Send a primary device attributes request
            Terminal.Out("\e[c");

            // Expected response: <ESC> [ ? [<attr> ; <attr> ; ... <attr>] c
            // Attributes are a semicolon seperated list
            Terminal.Read(buffer);

            // Sixel support is indicated with attribute 4, see https://vt100.net/docs/vt510-rm/DA1.html
            // Trim the beginning and end of the response to get just the attribute list
            var response = Encoding.ASCII.GetString(buffer).TrimEnd((char)0)[2..^1];
            var supportsSixel = response.Split(';').Contains("4");

            _logger.LogDebug(
                "Supports Sixel {Support}, Response: {Response}",
                supportsSixel,
                Util.Sanitize(response)
            );

            return supportsSixel;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Prepares the terminal to get it in to a state where it will definitely respond to queries.
    /// Some terminals will not respond to queries if in the middle of a synchronized update.
    /// </summary>
    private void PrepareTerminal()
    {
        if (IsSynchronizedOutputSupported.Value)
        {
            Terminal.Out(TerminalGraphics.EndSynchronizedUpdate());
        }
    }

    private static byte[] RentBuffer()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(128);
        Array.Fill(buffer, (byte)0);
        return buffer;
    }

    private void DiscardExtraResponse()
    {
        var buffer = RentBuffer();
        try
        {
            _logger.LogDebug("Reading extra device attr response to discard");
            // Got a response to the check, so read and throw away the device attributes response as well
            _ = Terminal.Read(buffer);
            _logger.LogDebug("Discarded extra response");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public record struct TerminalSizeInfo(
        int Height,
        int Width,
        int Rows,
        int Columns,
        int PixelsPerRow,
        int PixelsPerColumn
    )
    {
        public readonly double CellAspectRatio => PixelsPerColumn / (double)PixelsPerRow;

        public int RowsToPixels(int rows) => rows * PixelsPerRow;

        public int ColumnsToPixels(int columns) => columns * PixelsPerColumn;
    };
}
