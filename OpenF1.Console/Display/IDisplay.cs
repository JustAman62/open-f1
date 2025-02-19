using Spectre.Console.Rendering;

namespace OpenF1.Console;

public interface IDisplay
{
    Screen Screen { get; }

    Task<IRenderable> GetContentAsync();

    /// <summary>
    /// Called after the content from <see cref="GetContentAsync"/> has been drawn to the terminal.
    /// Intended for use cases where overwriting whats been drawn is required, such as for terminal graphics.
    /// </summary>
    /// <returns>A task that completes when the drawing has completed</returns>
    Task PostContentDrawAsync() => Task.CompletedTask;
}
