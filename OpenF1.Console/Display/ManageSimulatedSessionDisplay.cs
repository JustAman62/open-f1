using OpenF1.Console;
using Spectre.Console;
using Spectre.Console.Rendering;

public class ManageSimulatedSessionDisplay(IJsonTimingClient jsonTimingClient) : IDisplay
{
    public Screen Screen => Screen.ManageSimulatedSession;

    public Task<IRenderable> GetContentAsync()
    {
        var rows = new Rows(
            new Text($"Simulation Status: {jsonTimingClient.ExecuteTask?.Status.ToString() ?? "No Simulation Running"}")
        );

        return Task.FromResult<IRenderable>(new Panel(rows).Expand());
    }
}
