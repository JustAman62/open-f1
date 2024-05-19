namespace OpenF1.Data;

public class RaceControlMessageProcessor(INotifyService notifyService) : IProcessor<RaceControlMessageDataPoint>
{
    public RaceControlMessageDataPoint Latest { get; private set; } = new();

    public void Process(RaceControlMessageDataPoint data)
    {
        foreach (var message in data.Messages)
        {
            var added = Latest.Messages.TryAdd(message.Key, message.Value);
            if (added && (!message.Value.Message?.StartsWith("WAVED BLUE FLAG", StringComparison.OrdinalIgnoreCase) ?? true))
            {
                // New race control messages are important, so alert the user
                notifyService.SendNotification();
            }
        }
    }
}
