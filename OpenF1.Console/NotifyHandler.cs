using OpenF1.Data;

namespace OpenF1.Console;

public class NotifyHandler : INotifyHandler
{
    public Task OnNotificationAsync()
    {
        // When notifications are received, send a ASCII BEL to the console to make a noise to alert the user.
        System.Console.Write("\u0007");
        return Task.CompletedTask;
    }
}
