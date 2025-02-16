using Spectre.Console.Rendering;

namespace OpenF1.Console;

public interface IDisplay
{
    Screen Screen { get; }

    Task<IRenderable> GetContentAsync();
}
