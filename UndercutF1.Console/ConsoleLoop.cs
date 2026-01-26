using System.Buffers;
using System.Diagnostics;
using InhibitSleep.Net;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Advanced;
using Spectre.Console.Rendering;
using UndercutF1.Console.Graphics;

namespace UndercutF1.Console;

public class ConsoleLoop(
    State state,
    IEnumerable<IDisplay> displays,
    IEnumerable<IInputHandler> inputHandlers,
    IHostApplicationLifetime hostApplicationLifetime,
    TerminalInfoProvider terminalInfo,
    IOptions<ConsoleOptions> options,
    ILogger<ConsoleLoop> logger
) : BackgroundService
{
    private const long TargetFrameTimeMs = 150;
    private const byte ESC = 27; //0x1B
    private const byte CSI = 91; //0x5B [
    private const byte ARG_SEP = 59; //0x3B ;
    private const byte FE_START = 79; //0x4F

    private string _previousDraw = string.Empty;
    private bool _stopped = false;
    private int _slowFrameReports = 0;
    private SleepInhibitor? _sleepInhibitor;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Immediately yield to ensure all the other hosted services start as expected
        await Task.Yield();

        await SetupTerminalAsync(cancellationToken);

        var contentPanel = new Panel("Undercut F1").Expand().RoundedBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Content", contentPanel),
            new Layout("Footer")
        );
        layout["Footer"].Size = 1;

        var stopwatch = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Restart();

            await SetupBufferAsync(cancellationToken);
            await HandleInputsAsync(cancellationToken);

            if (state.CurrentScreen == Screen.Shutdown)
            {
                await StopAsync(cancellationToken);
                return;
            }

            var display = displays.SingleOrDefault(x => x.Screen == state.CurrentScreen);

            try
            {
                contentPanel = display is not null
                    ? await display.GetContentAsync()
                    : new Panel($"Unknown Display Selected: {state.CurrentScreen}").Expand();
                layout["Content"].Update(contentPanel);

                UpdateInputFooter(layout);

                // Unix rawmode + Windows terminals need CRLFs, but Environment.NewLine differs and is used by Spectre
                var output = AnsiConsole
                    .Console.ToAnsi(layout)
                    .Replace(Environment.NewLine, "\r\n");

                if (terminalInfo.IsSynchronizedOutputSupported.Value)
                {
                    await Terminal.OutAsync(
                        TerminalGraphics.BeginSynchronizedUpdate(),
                        cancellationToken
                    );
                }

                var shouldDraw = _previousDraw != output;

                if (_previousDraw != output)
                {
                    await Terminal.OutAsync(output, cancellationToken);
                    _previousDraw = output;
                }

                if (display is not null)
                {
                    await display.PostContentDrawAsync(shouldDraw);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rendering screen: {CurrentScreen}", state.CurrentScreen);
                await DisplayErrorScreenAsync(ex);
            }

            if (terminalInfo.IsSynchronizedOutputSupported.Value)
            {
                await Terminal.OutAsync(
                    TerminalGraphics.EndSynchronizedUpdate(),
                    cancellationToken
                );
            }

            stopwatch.Stop();

            if (_slowFrameReports < 10 && stopwatch.ElapsedMilliseconds > 100)
            {
                logger.LogWarning(
                    "Frame time for screen {Screen} exceeded 100ms: {Milliseconds}ms",
                    state.CurrentScreen,
                    stopwatch.ElapsedMilliseconds
                );
                _slowFrameReports++;
            }

            var timeToDelay = TargetFrameTimeMs - stopwatch.ElapsedMilliseconds;
            if (timeToDelay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeToDelay), cancellationToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Don't clean up the terminal multiple times.
        if (_stopped)
        {
            await base.StopAsync(cancellationToken);
            hostApplicationLifetime.StopApplication();
            return;
        }

        await Terminal.OutLineAsync("Exiting undercutf1...", CancellationToken.None);
        logger.LogInformation("ConsoleLoop Stopping.");

        await Terminal.OutAsync(
            ControlSequences.ClearScreen(ClearMode.Full),
            CancellationToken.None
        );
        await Terminal.OutAsync(ControlSequences.SetCursorVisibility(true), CancellationToken.None);
        Terminal.DisableRawMode();
        await Terminal.OutAsync(
            ControlSequences.SetScreenBuffer(ScreenBuffer.Main),
            CancellationToken.None
        );

        _sleepInhibitor?.ReleaseInhibition();

        _stopped = true;

        await base.StopAsync(cancellationToken);
        hostApplicationLifetime.StopApplication();
    }

    private async Task SetupTerminalAsync(CancellationToken cancellationToken)
    {
        await Terminal.OutAsync(
            ControlSequences.SetScreenBuffer(ScreenBuffer.Alternate),
            cancellationToken
        );
        Terminal.EnableRawMode();
        await Terminal.OutAsync(ControlSequences.SetCursorVisibility(false), cancellationToken);
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(0, 0), cancellationToken);
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);

        if (options.Value.PreventDisplaySleep)
        {
            logger.LogInformation(
                "{OptionName} is enabled, attempting to prevent device sleep",
                nameof(ConsoleOptions.PreventDisplaySleep)
            );
            _sleepInhibitor ??= new("undercutf1");
            _sleepInhibitor.InhibitSleep();
        }
    }

    private static async Task SetupBufferAsync(CancellationToken cancellationToken) =>
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(0, 0), cancellationToken);

    private async Task DisplayErrorScreenAsync(Exception exception)
    {
        try
        {
            await SetupBufferAsync(CancellationToken.None);
            var exceptionPanel = new Panel(
                new Rows(
                    new Text($"Failed to render screen {state.CurrentScreen}"),
                    new Text(exception.ToString())
                )
            )
            {
                Height = Terminal.Size.Height - 1,
            };

            var output = AnsiConsole
                .Console.ToAnsi(exceptionPanel)
                .Replace(Environment.NewLine, "\r\n");

            await Terminal.OutAsync(output);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to display error screen (things must be bad)");
        }
    }

    private void UpdateInputFooter(Layout layout)
    {
        var commandDescriptions = inputHandlers
            .Where(x => x.IsEnabled && x.ApplicableScreens.Contains(state.CurrentScreen))
            .OrderBy(x => x.Sort)
            .Select(x => $"[[{x.DisplayKeys.ToDisplayCharacters()}]] {x.Description}");

        var columns = new Columns(commandDescriptions.Select(x => new Markup(x)));
        columns.Collapse();
        layout["Footer"].Update(columns);
    }

    private async Task HandleInputsAsync(CancellationToken cancellationToken = default)
    {
        var inputBuffer = ArrayPool<byte>.Shared.Rent(8);
        Array.Fill<byte>(inputBuffer, 0);
        try
        {
#pragma warning disable RS0030 // Do not use banned APIs
            if (!System.Console.KeyAvailable)
            {
                // There is no input to read, so don't try to read it
                // Using the System.Console API here instead of timing out the Terminal.ReadAsync call below
                // Because there is a bug with Vezel.Cathode on Windows which causes lost bytes if you cancel a read
                // See https://github.com/vezel-dev/cathode/issues/165
                return;
            }
#pragma warning restore RS0030 // Do not use banned APIs

            await Terminal.ReadAsync(inputBuffer);
            logger.LogDebug("Read in input: {Input}", string.Join(',', inputBuffer));

            if (
                TryParseRawInput(
                    inputBuffer,
                    out var keyChar,
                    out var consoleKey,
                    out var modifiers,
                    out var times
                )
            )
            {
                var tasks = inputHandlers
                    .Where(handler =>
                        handler.IsEnabled
                        && handler.Keys.Contains(consoleKey)
                        && handler.ApplicableScreens.Contains(state.CurrentScreen)
                    )
                    // Repeat the input handlers for duplicate key presses
                    .SelectMany(x => Enumerable.Range(0, times).Select(_ => x))
                    .Select(x =>
                    {
                        try
                        {
                            return x.ExecuteAsync(
                                new ConsoleKeyInfo(
                                    keyChar,
                                    consoleKey,
                                    shift: modifiers.HasFlag(ConsoleModifiers.Shift),
                                    alt: false,
                                    control: modifiers.HasFlag(ConsoleModifiers.Control)
                                ),
                                cancellationToken
                            );
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "Failed to handle input {Key} for {Name}",
                                consoleKey,
                                x.Description
                            );
                            return Task.CompletedTask;
                        }
                    });
                await Task.WhenAll(tasks);
            }
        }
        catch (OperationCanceledException)
        {
            // No input to read, so skip
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle input");
            return;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inputBuffer);
        }
    }

    /// <summary>
    /// Parses raw input from the console in to the appropriate console character.
    /// Intended to parse control sequences (like those for arrow keys) in to the relevant console character.
    /// </summary>
    /// <remarks>
    /// See https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797 for escape code reference.
    /// </remarks>
    /// <param name="bytes">The bytes from the input to parse</param>
    /// <returns>
    /// A tuple of (keyChar, consoleKey) if the input can be parsed.
    /// <c>null</c> if the input is not a simple character,
    /// and should be treated as an actual escape sequence.
    /// </returns>
    private bool TryParseRawInput(
        byte[] bytes,
        out char keyChar,
        out ConsoleKey consoleKey,
        out ConsoleModifiers modifiers,
        out int times
    )
    {
        times = 1;

        switch (bytes)
        {
            case [ESC, CSI, ..]: // An ANSI escape sequence starting with a CSI (Control Sequence Introducer)
                switch (bytes[2..])
                {
                    // Keyboard strings
                    // these are mappings from keyboard presses (like shift+arrow)
                    case [49, ARG_SEP, 50, var key, ..]:
                        // These will be treated as uppercase chars
                        keyChar = (char)key;
                        consoleKey = key switch
                        {
                            68 => ConsoleKey.LeftArrow,
                            65 => ConsoleKey.UpArrow,
                            66 => ConsoleKey.DownArrow,
                            67 => ConsoleKey.RightArrow,
                            _ => default,
                        };
                        modifiers = ConsoleModifiers.Shift;
                        if (consoleKey == default)
                        {
                            logger.LogInformation(
                                "Unknown CSI keyboard string: {Seq}",
                                string.Join('|', bytes[2..])
                            );
                            return false;
                        }
                        return true;
                    // These are arrow keys on Windows (for some reason, different on mac/linux)
                    case [var key, ..] when key >= 65 && key <= 68:
                        keyChar = default;
                        consoleKey = key switch
                        {
                            68 => ConsoleKey.LeftArrow,
                            65 => ConsoleKey.UpArrow,
                            66 => ConsoleKey.DownArrow,
                            67 => ConsoleKey.RightArrow,
                            _ => default,
                        };
                        modifiers = ConsoleModifiers.None;
                        if (consoleKey == default)
                        {
                            logger.LogInformation(
                                "Unknown CSI keyboard string (direct): {Seq}",
                                string.Join('|', bytes[2..])
                            );
                            return false;
                        }
                        return true;
                }
                logger.LogInformation("Unknown CSI sequence: {Seq}", string.Join('|', bytes[2..]));
                break;
            case [ESC, FE_START, var key, ..]: // An escape sequence for terminal cursor control via Fe escape codes
                keyChar = default;
                consoleKey = key switch
                {
                    68 => ConsoleKey.LeftArrow,
                    65 => ConsoleKey.UpArrow,
                    66 => ConsoleKey.DownArrow,
                    67 => ConsoleKey.RightArrow,
                    _ => default,
                };
                modifiers = ConsoleModifiers.None;
                if (consoleKey == default)
                {
                    logger.LogInformation(
                        "Unknown FE escape sequence: {Seq}",
                        string.Join('|', bytes[2..])
                    );
                    return false;
                }
                return true;
            case [ESC, 0, ..]: // Just the escape key
                keyChar = (char)ESC;
                consoleKey = ConsoleKey.Escape;
                modifiers = ConsoleModifiers.None;
                return true;
            case [ESC, ..]:
                logger.LogInformation(
                    "Unknown esc sequence: {Seq} ({Str})",
                    string.Join('|', bytes[1..]),
                    Util.Sanitize(bytes)
                );
                break;
            case [var key, .. var remaining]: // Just a normal key press
                keyChar = (char)key;
                consoleKey = (ConsoleKey)char.ToUpperInvariant(keyChar);
                modifiers = char.IsUpper(keyChar) ? ConsoleModifiers.Shift : ConsoleModifiers.None;
                // How many duplicate presses of the key are there. This indicates a key being held down, so repeat the action
                times = 1 + remaining.Count(k => k == key);
                return true;
            default:
                logger.LogInformation("Unknown input: {Input}", string.Join('|', bytes));
                break;
        }
        keyChar = default;
        consoleKey = default;
        modifiers = default;
        return false;
    }
}
