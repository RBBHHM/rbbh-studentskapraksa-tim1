using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RBI.Authentication.Internal;

internal sealed class RbiOidcRefreshTokenClient(
    IOptionsMonitor<OpenIdConnectOptions> optionsMonitor,
    ILogger<RbiOidcRefreshTokenClient> logger) : IRbiRefreshTokenClient
{
    private const int MaximumPayloadBytes = 64 * 1024;

    public async ValueTask<RbiRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = optionsMonitor.Get(RbiAuthenticationSchemes.OpenIdConnect);
            if (options.ConfigurationManager is null)
                return RbiRefreshResult.TemporarilyUnavailable;

            var configuration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
                return RbiRefreshResult.TemporarilyUnavailable;

            using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = options.ClientId!,
                    ["client_secret"] = options.ClientSecret!,
                    ["refresh_token"] = refreshToken,
                }),
            };
            // Enforce the payload bound before buffering, and retain the backchannel
            // deadline while streaming (HttpClient ends its timeout at headers).
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(options.Backchannel.Timeout);
            using var response = await options.Backchannel.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            var payload = await ReadPayloadAsync(response, deadline.Token);

            if (response.IsSuccessStatusCode &&
                payload is { ExpiresIn: > 0 } &&
                !string.IsNullOrWhiteSpace(payload.AccessToken) &&
                (payload.RefreshToken is null || !string.IsNullOrWhiteSpace(payload.RefreshToken)))
            {
                return RbiRefreshResult.Success(
                    payload.AccessToken,
                    payload.RefreshToken,
                    TimeSpan.FromSeconds(payload.ExpiresIn));
            }

            if (response.StatusCode == HttpStatusCode.BadRequest &&
                string.Equals(payload?.Error, "invalid_grant", StringComparison.Ordinal))
            {
                logger.LogInformation("The identity session can no longer be refreshed.");
                return RbiRefreshResult.ReauthenticationRequired;
            }

            logger.LogWarning(
                "The identity provider could not refresh the user session. StatusCode={StatusCode}",
                (int)response.StatusCode);
            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? RbiRefreshResult.ReauthenticationRequired
                : RbiRefreshResult.TemporarilyUnavailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or OperationCanceledException)
        {
            logger.LogWarning(
                "The identity provider is temporarily unavailable during session refresh. FailureType={FailureType}",
                exception.GetType().Name);
            return RbiRefreshResult.TemporarilyUnavailable;
        }
    }

    private static async Task<RefreshPayload?> ReadPayloadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0 or > MaximumPayloadBytes)
            return null;

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[MaximumPayloadBytes + 1];
            var bytesRead = 0;

            while (bytesRead < buffer.Length)
            {
                var count = await stream.ReadAsync(
                    buffer.AsMemory(bytesRead, buffer.Length - bytesRead),
                    cancellationToken);
                if (count == 0)
                    break;

                bytesRead += count;
            }

            if (bytesRead is 0 or > MaximumPayloadBytes)
                return null;

            return JsonSerializer.Deserialize<RefreshPayload>(buffer.AsSpan(0, bytesRead));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record RefreshPayload(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("error")] string? Error);
}
