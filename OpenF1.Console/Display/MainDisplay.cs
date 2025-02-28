using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class MainDisplay() : IDisplay
{
    public Screen Screen => Screen.Main;

    private readonly FigletFont _font = FigletFont.Load(
        Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenF1.Console.slant.flf")!
    );

    public Task<IRenderable> GetContentAsync()
    {
        var title = new FigletText(_font, "OPEN F1").Centered();

        var content = new Markup(
            """
            Welcome to [bold italic]Open F1 Console[/].

            To start a live timing session, press [bold]S[/] then [bold]L[/].
            To start a replay a previously recorded/imported session, press [bold]S[/] then [bold]F[/].

            Once a session is started, navigate to the Timing Tower using [bold]T[/]
            Then use the Arrow Keys [bold]◄[/]/[bold]►[/] to switch between timing pages.
            Use [bold]N[/]/[bold]M[/] to adjust the stream delay, and [bold]▲[/]/[bold]▼[/] keys to use the cursor.
            Press Shift with these keys to adjust by a higher amount.

            You can download old session data from Formula 1 by running:
            > openf1-console import
            """
        );

        var footer = new Text(
            $"""
            GitHub: https://github.com/JustAman62/open-f1
            Version: {ThisAssembly.AssemblyInformationalVersion}
            """
        );

        var layout = new Layout("Content").SplitRows(
            new Layout("Title", title).Size(8),
            new Layout("Content", content),
            new Layout("Footer", footer).Size(2)
        );
        var panel = new Panel(layout).Expand().RoundedBorder();

        return Task.FromResult<IRenderable>(panel);
    }
}
