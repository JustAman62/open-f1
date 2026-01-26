using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public sealed class Formula1Account
{
    private readonly IOptionsMonitor<LiveTimingOptions> _options;
    private readonly ILogger<Formula1Account> _logger;

    public Formula1Account(
        IOptionsMonitor<LiveTimingOptions> options,
        ILogger<Formula1Account> logger
    )
    {
        _options = options;
        _logger = logger;

        _options.OnChange((opts) => Refresh(opts.Formula1AccessToken));

        Refresh(_options.CurrentValue.Formula1AccessToken);
    }

    public string? AccessToken { get; private set; }
    public TokenPayload? Payload { get; private set; }
    public AuthenticationResult IsAuthenticated { get; private set; }

    public void Refresh(string? token)
    {
        var authResult = CheckToken(token, out var payload);
        Payload = payload;
        IsAuthenticated = authResult;
        AccessToken =
            authResult == AuthenticationResult.Success
                ? SubscriptionTokenFromAccessToken(token!)
                : null;
    }

    private AuthenticationResult CheckToken(string? token, out TokenPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticationResult.NoToken;

        try
        {
            payload = GetTokenPayloadFromToken(token);
            if (payload is null)
                return AuthenticationResult.InvalidToken;

            if (payload.SubscriptionStatus != "active")
            {
                _logger.LogError(
                    "Formula 1 Access Token does not have an active subscription status (Value: {Status}). If you've recently subscribed, you may want to try logging in again. This token cannot be used for authenticated with the live timing feed. An F1 TV subscription is required for the additional live timing feeds, but no login is required for normal functionality.",
                    payload.SubscriptionStatus
                );
                return AuthenticationResult.InvalidSubscriptionStatus;
            }

            if (payload.Expiry < DateTimeOffset.UtcNow)
            {
                _logger.LogError(
                    "Formula 1 Access Token expired on {Date}, please login again.",
                    payload.Expiry
                );
                return AuthenticationResult.ExpiredToken;
            }

            return AuthenticationResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read data in access token");
            return AuthenticationResult.InvalidToken;
        }
    }

    private TokenPayload? GetTokenPayloadFromToken(string token)
    {
        var subscriptionToken = SubscriptionTokenFromAccessToken(token)!;

        // The token is split in to three parts, a header, body, and sig. We only want to read the body.
        var tokenPart = subscriptionToken.Split('.')[1];

        // For some reason, the base64 encoded string sometimes doesn't have enough padding chars at the end
        // Base64 strings should be a multiple of 4
        var missingPaddingChars = tokenPart.Length % 4;
        if (missingPaddingChars > 0)
        {
            _logger.LogDebug("Adding {Count} extra padding chars", missingPaddingChars);
            tokenPart += new string('=', 4 - missingPaddingChars);
        }

        var tokenPayload = JsonSerializer.Deserialize(
            Convert.FromBase64String(tokenPart),
            TimingDataSerializerContext.Raw.TokenPayload
        );
        _logger.LogDebug("F1 TV Token Details: {Token}", tokenPayload);
        return tokenPayload;
    }

    private string? SubscriptionTokenFromAccessToken(string token)
    {
        var jsonString = Uri.UnescapeDataString(token);
        return JsonNode.Parse(jsonString)?["data"]?["subscriptionToken"]?.GetValue<string>();
    }

    public sealed record TokenPayload(
        string SubscriptionStatus,
        string? SubscribedProduct,
        int Exp,
        int Iat
    )
    {
        public DateTimeOffset Expiry => DateTimeOffset.FromUnixTimeSeconds(Exp);

        public DateTimeOffset IssuedAt => DateTimeOffset.FromUnixTimeSeconds(Iat);
    }

    public enum AuthenticationResult
    {
        Success,
        NoToken,
        InvalidToken,
        InvalidSubscriptionStatus,
        ExpiredToken,
    }
}
