using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class MainDisplay() : IDisplay
{
    public Screen Screen => Screen.Main;

    public Task<IRenderable> GetContentAsync()
    {
        var text = new FigletText("OPEN F1").Centered();
        var panel = new Panel(text)
            .Expand()
            .SafeBorder();

        return Task.FromResult<IRenderable>(panel);
    }
}
