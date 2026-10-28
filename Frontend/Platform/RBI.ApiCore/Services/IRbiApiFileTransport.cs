namespace RBI.ApiCore;

/// <summary>Extends the JSON transport with bounded file downloads and multipart POSTs.</summary>
/// <remarks>Typed clients may request this interface in their constructor. The same
/// origin validation, token session, correlation, timeout and error pipeline is used.
/// No token or raw HTTP response is exposed to consumers.</remarks>
public interface IRbiApiFileTransport : IRbiApiTransport
{
    /// <summary>Downloads a file into bounded memory, preserving safe attachment metadata.</summary>
    /// <param name="requestUri">A path relative to the configured API origin.</param>
    /// <param name="maxBytes">Maximum response bytes, from 1 to 268435456. Default is 32 MiB.</param>
    /// <param name="cancellationToken">Cancellation for headers and the entire body read.</param>
    Task<RbiApiResult<RbiApiFile>> DownloadAsync(string requestUri,
        int maxBytes = 33554432, CancellationToken cancellationToken = default);

    /// <summary>Posts multipart content and reads a typed JSON result without replaying the upload.</summary>
    /// <remarks>Consumes and disposes content and its streams on every outcome,
    /// including invalid arguments. Callers must supply their own upload size limits.
    /// POST is never retried, even with the Standard resilience profile.</remarks>
    Task<RbiApiResult<TResponse>> PostMultipartAsync<TResponse>(string requestUri,
        MultipartFormDataContent content, CancellationToken cancellationToken = default);
}
