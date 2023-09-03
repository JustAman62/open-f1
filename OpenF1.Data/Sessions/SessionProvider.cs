using System.Text.Json.Nodes;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

/// <summary>
/// Provides the current session being output by the F1 live timing service
/// </summary>
/// <remarks>
/// The session name is cached for 30 minutes before being refetched.
/// </remarks>
public sealed class SessionProvider : ISessionProvider
{
    private readonly ILogger<SessionProvider> _logger;
    private string? _sessionName;
    private DateTimeOffset _lastFetch;

    public SessionProvider(ILogger<SessionProvider> logger) => _logger = logger;

    public async ValueTask<string> GetSessionName()
    {
        // If we have cached the name, and it was cached less than 30 minutes ago, then return it straight away
        if (_sessionName is not null && _lastFetch - DateTimeOffset.UtcNow < TimeSpan.FromMinutes(30))
        {
            return _sessionName;
        }

        try 
        {
            var httpClient = new HttpClient();
            var res  = await httpClient.GetFromJsonAsync<JsonObject>($"https://livetiming.formula1.com/static/SessionInfo.json?{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            _sessionName = res?["Meeting"]?["Name"]?.ToString();
            _lastFetch = DateTimeOffset.UtcNow;

            _logger.LogInformation($"Fetched the current session: {_sessionName}");

            return _sessionName!;
        }
        catch (Exception)
        {
            if (_sessionName is not null) return _sessionName;
            throw;
        }

    }
}
