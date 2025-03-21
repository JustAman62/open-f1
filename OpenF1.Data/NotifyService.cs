using Microsoft.Extensions.Options;

namespace OpenF1.Data;

public class NotifyService(
    IEnumerable<INotifyHandler> handlers,
    IOptions<LiveTimingOptions> options
) : INotifyService
{
    /// <inheritdoc />
    public void SendNotification()
    {
        if (!options.Value.Notify)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            // Fire-and-forget each task
            // In the future, we might push these to a queue and handle them inside a IHostedService
            _ = Task.Run(handler.OnNotificationAsync);
        }
    }
}
