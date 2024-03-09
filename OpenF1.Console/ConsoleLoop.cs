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
    private const long TargetFrameTimeMs = 100;

    public async Task ExecuteAsync()
    {
        var contentPanel = new Panel("Open F1").Expand().SafeBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Content", contentPanel).Ratio(10),
            new Layout("Footer")
        );
        layout["Footer"].Size = 3;

        AnsiConsole.Cursor.Hide();
        var stopwatch = Stopwatch.StartNew();
        while (true)
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

            AnsiConsole.Clear();
            AnsiConsole.Write(layout);

            stopwatch.Stop();
            var timeToDelay = TargetFrameTimeMs - stopwatch.ElapsedMilliseconds;
            if (timeToDelay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeToDelay)).ConfigureAwait(false);
            }
        }
    }

    public async Task ShowAndHandleInputs(Layout layout)
    {
        var commandDescriptions = inputHandlers
            .Where(x =>
                x.ApplicableScreens is null || x.ApplicableScreens.Contains(state.CurrentScreen)
            )
            .Select(x => $"[{x.ConsoleKey}] {x.Description}");
        var footerPanel = new Panel(
            new Markup(string.Join(' ', commandDescriptions).EscapeMarkup())
        )
        {
            Header = new PanelHeader("Commands")
        };
        layout["Footer"].Update(footerPanel.Expand());

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
}
