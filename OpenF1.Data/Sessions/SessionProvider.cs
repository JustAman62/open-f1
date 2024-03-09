using System.Text.Json.Nodes;
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
            var currentDateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var res  = await httpClient.GetStringAsync($"https://livetiming.formula1.com/static/SessionInfo.json?{currentDateTime}");
            var jsonObject = JsonObject.Parse(res);
            _sessionName = $"{jsonObject?["Meeting"]?["Name"]} {jsonObject?["Name"]}";
            _lastFetch = DateTimeOffset.UtcNow;

            _logger.LogInformation($"Fetched the current session: {_sessionName}");

            return _sessionName!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception thrown trying to fetch session name");
            if (_sessionName is not null) return _sessionName;
            throw;
        }
    }
}
