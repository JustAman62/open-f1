public interface IJsonTimingClient 
{
    public Task? ExecuteTask { get; }

    public IEnumerable<string> GetFileNames();
    
    public Task StartAsync(string fileName);

    public Task StopAsync();
}
