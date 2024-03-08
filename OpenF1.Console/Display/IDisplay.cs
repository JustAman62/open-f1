using Spectre.Console.Rendering;

namespace OpenF1.Console;

public interface IDisplay
{
    public Screen Screen { get; }

    public Task<IRenderable> GetContentAsync();
}
