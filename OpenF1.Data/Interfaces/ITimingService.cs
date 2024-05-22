using Microsoft.Extensions.Hosting;

namespace OpenF1.Data;

public interface ITimingService : IHostedService
{
    public List<(string type, string? data, DateTimeOffset timestamp)> GetQueueSnapshot();

    public Task EnqueueAsync(string type, string? data, DateTimeOffset timestamp);

    public int GetRemainingWorkItems();

    public void ProcessSubscriptionData(string res);
}
