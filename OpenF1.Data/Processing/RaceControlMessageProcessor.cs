namespace OpenF1.Data;

public class RaceControlMessageProcessor : IProcessor<RaceControlMessageDataPoint>
{
    public RaceControlMessageDataPoint RaceControlMessages { get; private set; } = new();

    public void Process(RaceControlMessageDataPoint data)
    {
        foreach (var message in data.Messages)
        {
            RaceControlMessages.Messages.TryAdd(message.Key, message.Value);
        }
    }
}
