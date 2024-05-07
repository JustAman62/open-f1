using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class ConsoleLoop(
    State state,
    IEnumerable<IDisplay> displays,
    IEnumerable<IInputHandler> inputHandlers
)
{
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private const long TargetFrameTimeMs = 100;

    public async Task ExecuteAsync()
    {
        _cts = new CancellationTokenSource();

        var contentPanel = new Panel("Open F1").Expand().SafeBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Content", contentPanel).Ratio(10),
            new Layout("Footer")
        );
        layout["Footer"].Size = 2;

        AnsiConsole.Cursor.Hide();
        
        var stopwatch = Stopwatch.StartNew();
        while (!_cts.IsCancellationRequested)
        {
            stopwatch.Restart();
            await ShowAndHandleInputs(layout).ConfigureAwait(false);

            if (state.CurrentScreen == Screen.Shutdown)
            {
                AnsiConsole.Clear();
                return;
            }

            var display = displays.SingleOrDefault(x => x.Screen == state.CurrentScreen);

            try
            {
                contentPanel = display is not null
                    ? await display.GetContentAsync().ConfigureAwait(false)
                    : new Panel($"Unknown Display Selected: {state.CurrentScreen}").Expand();
            }
            catch (Exception ex)
            {
                contentPanel = new Panel($"Exception: {ex.ToString().EscapeMarkup()}").Expand();
            }

            layout["Content"].Update(contentPanel);

            AnsiConsole.Cursor.SetPosition(0, 0);
            AnsiConsole.Cursor.Hide();
            AnsiConsole.Write(layout);

            stopwatch.Stop();
            var timeToDelay = TargetFrameTimeMs - stopwatch.ElapsedMilliseconds;
            if (timeToDelay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeToDelay)).ConfigureAwait(false);
            }
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    private async Task ShowAndHandleInputs(Layout layout)
    {
        var commandDescriptions = inputHandlers
            .Where(x => x.IsEnabled && x.ApplicableScreens.Contains(state.CurrentScreen))
            .OrderBy(x => x.Sort)
            .Select(x => $"[{GetConsoleKeyCharacter(x.ConsoleKey)}] {x.Description}");

        var columns = new Columns(commandDescriptions.Select(x => new Text(x)));
        layout["Footer"].Update(columns);

        if (System.Console.KeyAvailable)
        {
            var key = System.Console.ReadKey();
            var tasks = inputHandlers
                .Where(x =>
                    x.ConsoleKey == key.Key
                    && (
                        x.ApplicableScreens is null
                        || x.ApplicableScreens.Contains(state.CurrentScreen)
                    )
                )
                .Select(x => x.ExecuteAsync(key));
            await Task.WhenAll(tasks);
        }
    }

    private string GetConsoleKeyCharacter(ConsoleKey consoleKey) =>
        consoleKey switch
        {
            ConsoleKey.Escape => "Esc",
            ConsoleKey.UpArrow => "▲",
            ConsoleKey.DownArrow => "▼",
            ConsoleKey.LeftArrow => "◄",
            ConsoleKey.RightArrow => "►",
            _ => consoleKey.ToString()
        };
}
