using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class ConsoleLoop(
    State state,
    IEnumerable<IDisplay> displays,
    IEnumerable<IInputHandler> inputHandlers
)
{
    public async Task ExecuteAsync(LiveDisplayContext ctx)
    {
        var contentPanel = new Panel("Open F1").Expand().SafeBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Content", contentPanel).Ratio(10),
            new Layout("Footer")
        );
        layout["Footer"].Size = 3;

        ctx.UpdateTarget(layout);

        while (true)
        {
            await ShowAndHandleInputs(layout);

            if (state.CurrentScreen == Screen.Shutdown)
            {
                AnsiConsole.Clear();
                return;
            }

            var display = displays.SingleOrDefault(x => x.Screen == state.CurrentScreen);
            contentPanel = display is not null
                ? await display.GetContentAsync()
                : new Panel($"Unknown Display Selected: {state.CurrentScreen}").Expand();

            layout["Content"].Update(contentPanel);
            AnsiConsole.Clear();
            ctx.Refresh();

            await Task.Delay(16);
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
