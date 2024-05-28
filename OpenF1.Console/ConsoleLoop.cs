using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class ConsoleLoop(
    State state,
    IEnumerable<IDisplay> displays,
    IEnumerable<IInputHandler> inputHandlers,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    private const long TargetFrameTimeMs = 100;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Immediately yield to ensure all the other hosted services start as expected
        await Task.Yield();

        var contentPanel = new Panel("Open F1").Expand().RoundedBorder() as IRenderable;
        var layout = new Layout("Root").SplitRows(
            new Layout("Content", contentPanel),
            new Layout("Footer")
        );
        layout["Footer"].Size = 1;

        AnsiConsole.Cursor.Hide();
        
        var stopwatch = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Restart();
            await ShowAndHandleInputs(layout).ConfigureAwait(false);

            if (state.CurrentScreen == Screen.Shutdown)
            {
                await StopAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var display = displays.SingleOrDefault(x => x.Screen == state.CurrentScreen);

            AnsiConsole.Cursor.SetPosition(0, 0);
            AnsiConsole.Cursor.Hide();
            try
            {
                contentPanel = display is not null
                    ? await display.GetContentAsync().ConfigureAwait(false)
                    : new Panel($"Unknown Display Selected: {state.CurrentScreen}").Expand();
                layout["Content"].Update(contentPanel);
                AnsiConsole.Write(layout);
            }
            catch (Exception ex)
            {
                AnsiConsole.Write(new Panel($"Exception: {ex.ToString().EscapeMarkup()}").Expand());
            }

            stopwatch.Stop();
            var timeToDelay = TargetFrameTimeMs - stopwatch.ElapsedMilliseconds;
            if (timeToDelay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeToDelay), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        System.Console.WriteLine("Exiting openf1-console...");
        AnsiConsole.Cursor.Show();
        hostApplicationLifetime.StopApplication();
    }

    private async Task ShowAndHandleInputs(Layout layout)
    {
        var commandDescriptions = inputHandlers
            .Where(x => x.IsEnabled && x.ApplicableScreens.Contains(state.CurrentScreen))
            .OrderBy(x => x.Sort)
            .Select(x => $"[{string.Join('/', x.Keys.Select(k => k.GetConsoleKeyCharacter()))}] {x.Description}");

        var columns = new Columns(commandDescriptions.Select(x => new Text(x)));
        layout["Footer"].Update(columns);

        if (System.Console.KeyAvailable)
        {
            var key = System.Console.ReadKey();
            var tasks = inputHandlers
                .Where(x =>
                    x.Keys.Contains(key.Key)
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
