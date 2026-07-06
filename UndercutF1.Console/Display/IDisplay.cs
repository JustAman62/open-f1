using Spectre.Console.Rendering;

namespace UndercutF1.Console;

public interface IDisplay
{
    Screen Screen { get; }

    Task<IRenderable> GetContentAsync();

    /// <summary>
    /// Called after the content from <see cref="GetContentAsync"/> has been drawn to the terminal.
    /// Intended for use cases where overwriting what's been drawn is required, such as for terminal graphics.
    /// </summary>
    /// <param name="forceDraw">If true, this function should result in a draw being made, ignoring any optimisation logic</param>
    /// <returns>A task that completes when the drawing has completed</returns>
    Task PostContentDrawAsync(bool forceDraw) => Task.CompletedTask;

    /// <summary>The app's default redraw interval, used by screens that don't override it.</summary>
    const int DefaultFrameIntervalMs = 150;

    /// <summary>
    /// How often this screen should be redrawn. A screen can override it to run faster than the
    /// app default without affecting any other screen's render rate.
    /// </summary>
    TimeSpan FrameInterval => TimeSpan.FromMilliseconds(DefaultFrameIntervalMs);
}
