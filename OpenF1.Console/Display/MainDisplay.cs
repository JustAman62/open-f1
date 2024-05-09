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
        var text = new FigletText(_font, "OPEN F1").Centered();
        var panel = new Panel(text).Expand().SafeBorder();

        return Task.FromResult<IRenderable>(panel);
    }
}
