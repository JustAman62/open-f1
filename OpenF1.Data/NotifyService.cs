
namespace OpenF1.Data;

public class NotifyService : INotifyService
{
    private Action? _action;

    /// <inheritdoc />
    public void RegisterNotificationHandler(Action action) => _action = action;
    
    /// <inheritdoc />
    public void SendNotification() => _action?.Invoke();
}
