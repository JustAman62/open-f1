namespace OpenF1.Data;

public interface IJsonTimingClient
{
    public Task? ExecuteTask { get; }

    /// <summary>
    /// Fetches the directories which contain suitable files for simulation. 
    /// Suitable directories contain a file named <c>live.txt</c> and <c>subscribe.txt</c>.
    /// </summary>
    /// <returns>A list of suitable directory paths</returns>
    public IEnumerable<string> GetDirectoryNames();
    
    /// <summary>
    /// Starts a simulation using the files inside the provided directory.
    /// The directory provided in <paramref name="directory"/> must contain a 
    /// file named <c>live.txt</c> and <c>subscribe.txt</c>
    /// </summary>
    /// <param name="directory">The directory to load the simulation files from.</param>
    /// <returns>A Task indicating when the simulation has been started.</returns>
    public Task StartAsync(string directory);

    public Task StopAsync();
}
