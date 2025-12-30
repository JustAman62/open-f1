namespace UndercutF1.Data;

public class RaceControlMessageProcessor(INotifyService notifyService)
    : IProcessor<RaceControlMessageDataPoint>
{
    public RaceControlMessageDataPoint Latest { get; private set; } = new();

    public void Process(RaceControlMessageDataPoint data)
    {
        foreach (var message in data.Messages)
        {
            var added = Latest.Messages.TryAdd(message.Key, message.Value);

            // Blue flag messages are silent, all other messages cause an audible notification
            var isSilent =
                string.IsNullOrWhiteSpace(message.Value.Message)
                || message.Value.Message.StartsWith(
                    "WAVED BLUE FLAG",
                    StringComparison.OrdinalIgnoreCase
                );

            if (added && !isSilent)
            {
                // New race control messages are important, so alert the user
                notifyService.SendNotification();
            }
        }
    }
}
