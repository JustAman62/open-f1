namespace OpenF1.Data;

public interface IJsonTimingClient
{
    /// <summary>
    /// The <see cref="Task"/> representing the current state of the simulation data loading process.
    /// </summary>
    Task? ExecuteTask { get; }

    /// <summary>
    /// Fetches the directories which contain suitable files for simulation.
    /// Suitable directories contain a file named <c>live.txt</c> and <c>subscribe.txt</c>.
    /// The directory name must be formed as <c>/[location]_[session_type]/</c> to support grouping of the directories.
    /// </summary>
    /// <returns>A dictionary of suitable directory paths, grouped by the location name to all sessions in that location.</returns>
    Task<
        Dictionary<(string Location, DateOnly Date), List<(string Session, string Directory)>>
    > GetDirectoryNamesAsync();

    /// <summary>
    /// Starts a simulation using the files inside the provided directory.
    /// The directory provided in <paramref name="directory"/> must contain a
    /// file named <c>live.txt</c> and <c>subscribe.txt</c>
    /// </summary>
    /// <param name="directory">The directory to load the simulation files from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> indicating that execution should be stopped.</param>
    /// <returns>A Task indicating when all simulation data has been sent to the <see cref="ITimingClient"/>.</returns>
    Task StartAsync(string directory, CancellationToken cancellationToken = default);
}
