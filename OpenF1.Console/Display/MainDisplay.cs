using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class MainDisplay() : IDisplay
{
    public Screen Screen => Screen.Main;

    private readonly FigletFont _font = FigletFont.Load(
        Path.Join(
            Directory.GetParent(AppContext.BaseDirectory)!.FullName,
            "/slant.flf"
        )
    );

    public Task<IRenderable> GetContentAsync()
    {
        var text = new FigletText(_font, "OPEN F1").Centered();
        var panel = new Panel(text).Expand().SafeBorder();

        return Task.FromResult<IRenderable>(panel);
    }
}
