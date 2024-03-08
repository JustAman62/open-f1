using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class ConsoleLoop(
    State state,
    IEnumerable<IInputHandler> inputHandlers,
    TestDisplay testDisplay
)
{
    public async Task ExecuteAsync(LiveDisplayContext ctx)
    {
        var contentPanel = new Panel("Open F1").Expand().SafeBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Open F1", contentPanel),
            new Layout("Footer")
        );

        ctx.UpdateTarget(layout);

        while (true)
        {
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

            contentPanel = await testDisplay.ExecuteAsync();

            layout["Open F1"].Update(contentPanel);
            ctx.Refresh();

            await Task.Delay(16);

            if (state.CurrentScreen == Screen.Shutdown)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("Shutting Down...");
                await Task.Delay(500);
            }
        }
    }
}
