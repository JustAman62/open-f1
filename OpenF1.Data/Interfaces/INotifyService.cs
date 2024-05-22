namespace OpenF1.Data;

public interface INotifyService
{
    /// <summary>
    /// Sends a notification to any registered <see cref="INotifyHandler"/>s.
    /// This should only be used to alert a user to some important action, and not to transmit any data.
    /// </summary>
    public void SendNotification();
}
