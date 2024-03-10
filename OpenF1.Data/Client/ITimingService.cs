namespace OpenF1.Data;

public interface ITimingService
{
    public TimeSpan Delay { get; set; }
    
    public Task StartAsync();
    public Task StopAsync();

    public List<(string type, string? data, DateTimeOffset timestamp)> GetQueueSnapshot();

    public void Enqueue(string type, string? data, DateTimeOffset timestamp);

    public void ProcessSubscriptionData(string res);
}
