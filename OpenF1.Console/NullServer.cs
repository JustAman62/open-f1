using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace OpenF1.Console;

/// <summary>
/// A empty <see cref="IServer"/> implementation which does nothing. 
/// Used when we want to have a ASP.NETCore app but with the actual server disabled.
/// </summary>
internal class NullServer : IServer
{
    public IFeatureCollection Features => new FeatureCollection();

    public void Dispose() { }

    public Task StartAsync<TContext>(
        IHttpApplication<TContext> application,
        CancellationToken cancellationToken
    )
        where TContext : notnull => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
