namespace OpenF1.Data;

public class NotifyService(IEnumerable<INotifyHandler> handlers) : INotifyService
{
    /// <inheritdoc />
    public void SendNotification()
    {
        foreach (var handler in handlers)
        {
            // Fire-and-forget each task
            // In the future, we might push these to a queue and handle them inside a IHostedService
            _ = handler.OnNotificationAsync();
        }
    }
}
