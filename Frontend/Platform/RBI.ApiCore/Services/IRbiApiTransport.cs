namespace RBI.ApiCore;

/// <summary>Sends JSON API requests and returns normalized, exception-safe results.</summary>
/// <remarks>
/// Network, HTTP, timeout, malformed-response and caller-cancellation outcomes are returned as failed results, never thrown.
/// Programming errors are thrown: a blank, absolute, scheme-relative or backslash-prefixed request URI raises
/// <see cref="ArgumentException"/> before any request is sent, because every request is relative to the configured
/// <c>BaseUrl</c> and a propagated user access token must never reach another host.
/// </remarks>
public interface IRbiApiTransport
{
    /// <summary>Sends a GET request and deserializes its successful JSON response.</summary>
    Task<RbiApiResult<TResponse>> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a request with an optional JSON body and deserializes its successful response.</summary>
    Task<RbiApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        object? jsonBody = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a request with an optional JSON body when no response value is required.</summary>
    Task<RbiApiResult> SendAsync(
        HttpMethod method,
        string requestUri,
        object? jsonBody = null,
        CancellationToken cancellationToken = default);
}
