using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public record TestDisplayOptions
{
    public string DisplayedText { get; set; } = "A";
}

public class TestDisplay(IOptions<TestDisplayOptions> options)
{
    public Task<IRenderable> ExecuteAsync()
    {
        var panel = new Panel(options.Value.DisplayedText).Expand().SafeBorder();
        
        return Task.FromResult<IRenderable>(panel);
    }
}
