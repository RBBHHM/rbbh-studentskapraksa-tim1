using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace RBI.ApiCore.Internal;

internal sealed class RbiApiTransport(
    HttpClient client,
    ILogger logger,
    string clientName,
    IRbiApiLogicalRequestHandler? logicalHandler = null) : IRbiApiFileTransport
{
    private const int MaxErrorBodyBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public Task<RbiApiResult<TResponse>> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Get, requestUri, null, cancellationToken);

    public Task<RbiApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        object? jsonBody = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<TResponse>(method, requestUri, jsonBody, null, cancellationToken);

    public async Task<RbiApiResult<TResponse>> PostMultipartAsync<TResponse>(
        string requestUri, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using (content)
            return await SendJsonAsync<TResponse>(HttpMethod.Post, requestUri, null, content, cancellationToken);
    }

    private async Task<RbiApiResult<TResponse>> SendJsonAsync<TResponse>(HttpMethod method,
        string requestUri, object? jsonBody, HttpContent? content, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var deadline = CreateDeadline(cancellationToken);
        var execution = await ExecuteAsync(method, requestUri, jsonBody, deadline.Token, cancellationToken, content);
        if (execution.Error is { } error) return RbiApiResult<TResponse>.Failure(error);

        using var response = execution.Response!;
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return Failure<TResponse>(new RbiApiError(
                RbiApiErrorKind.Unknown,
                response.StatusCode,
                "The API returned an empty response.",
                "This operation requires a response value.",
                ResolveTraceId(response)), method, stopwatch.Elapsed);
        }

        try
        {
            var value = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, deadline.Token);
            return value is null
                ? Failure<TResponse>(new RbiApiError(
                    RbiApiErrorKind.Unknown,
                    response.StatusCode,
                    "The API returned an invalid response.",
                    "The JSON response did not contain a value.",
                    ResolveTraceId(response)), method, stopwatch.Elapsed)
                : RbiApiResult<TResponse>.Success(value);
        }
        catch (JsonException)
        {
            return Failure<TResponse>(new RbiApiError(
                RbiApiErrorKind.Unknown,
                response.StatusCode,
                "The API returned an invalid response.",
                "The successful response body was not valid for the expected contract.",
                ResolveTraceId(response)), method, stopwatch.Elapsed);
        }
        catch (NotSupportedException)
        {
            return Failure<TResponse>(new RbiApiError(
                RbiApiErrorKind.Unknown,
                response.StatusCode,
                "The API returned an unsupported response.",
                "The response could not be read as the expected contract.",
                ResolveTraceId(response)), method, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RbiApiResult<TResponse>.Failure(CancellationError());
        }
        catch (OperationCanceledException)
        {
            return Failure<TResponse>(TimeoutError(), method, stopwatch.Elapsed);
        }
        catch (HttpRequestException exception)
        {
            return Failure<TResponse>(NetworkError(exception.StatusCode), method, stopwatch.Elapsed);
        }
        catch (IOException)
        {
            // The connection broke while the body was streaming (HttpIOException derives from IOException).
            return Failure<TResponse>(NetworkError(null), method, stopwatch.Elapsed);
        }
    }

    public async Task<RbiApiResult> SendAsync(
        HttpMethod method,
        string requestUri,
        object? jsonBody = null,
        CancellationToken cancellationToken = default)
    {
        using var deadline = CreateDeadline(cancellationToken);
        var execution = await ExecuteAsync(method, requestUri, jsonBody, deadline.Token, cancellationToken);
        if (execution.Error is { } error) return RbiApiResult.Failure(error);

        execution.Response!.Dispose();
        return RbiApiResult.Success();
    }

    public async Task<RbiApiResult<RbiApiFile>> DownloadAsync(string requestUri,
        int maxBytes = 33554432, CancellationToken cancellationToken = default)
    {
        if (maxBytes is < 1 or > 268435456) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        var stopwatch = Stopwatch.StartNew();
        using var deadline = CreateDeadline(cancellationToken);
        var execution = await ExecuteAsync(HttpMethod.Get, requestUri, null, deadline.Token, cancellationToken);
        if (execution.Error is { } error) return RbiApiResult<RbiApiFile>.Failure(error);
        using var response = execution.Response!;
        try
        {
            if (response.Content.Headers.ContentLength > maxBytes)
                return FileTooLarge();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[Math.Min(81920, maxBytes + 1)];
            while (buffer.Length <= maxBytes)
            {
                var remaining = maxBytes + 1 - (int)buffer.Length;
                var read = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, remaining)), deadline.Token);
                if (read == 0) break;
                buffer.Write(chunk, 0, read);
            }
            if (buffer.Length > maxBytes) return FileTooLarge();
            var disposition = response.Content.Headers.ContentDisposition;
            var name = disposition?.FileNameStar ?? disposition?.FileName;
            var basename = name?.Trim('"').Replace('\\', '/').Split('/').LastOrDefault();
            var safeName = new string((basename ?? string.Empty).Where(character =>
                !char.IsControl(character) && character is not '<' and not '>' and not ':' and not '"' and not '|' and not '?' and not '*').Take(255).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safeName) || safeName is "." or "..") safeName = "download";
            return RbiApiResult<RbiApiFile>.Success(new(buffer.ToArray(), response.Content.Headers.ContentType?.MediaType, safeName));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RbiApiResult<RbiApiFile>.Failure(CancellationError());
        }
        catch (OperationCanceledException)
        {
            return Failure<RbiApiFile>(TimeoutError(), HttpMethod.Get, stopwatch.Elapsed);
        }
        catch (HttpRequestException exception)
        {
            return Failure<RbiApiFile>(NetworkError(exception.StatusCode), HttpMethod.Get, stopwatch.Elapsed);
        }
        catch (IOException)
        {
            return Failure<RbiApiFile>(NetworkError(null), HttpMethod.Get, stopwatch.Elapsed);
        }

        RbiApiResult<RbiApiFile> FileTooLarge() => Failure<RbiApiFile>(new RbiApiError(
            RbiApiErrorKind.Unknown, response.StatusCode, "The file exceeds the configured download limit."), HttpMethod.Get, stopwatch.Elapsed);
    }

    // ResponseHeadersRead ends HttpClient's own timeout at the headers. Keep the
    // configured request deadline alive through logical handlers and body reads.
    private CancellationTokenSource CreateDeadline(CancellationToken callerToken)
    {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        deadline.CancelAfter(client.Timeout);
        return deadline;
    }

    private async Task<TransportExecution> ExecuteAsync(
        HttpMethod method,
        string requestUri,
        object? jsonBody,
        CancellationToken cancellationToken,
        CancellationToken callerToken,
        HttpContent? content = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        // Requests are always relative to the configured BaseUrl; an absolute or scheme-relative URI would
        // send the request (and any propagated bearer token) to a host the client was never configured for.
        // System.Uri trims leading whitespace before resolving network paths.
        // Validate that representation and the resolved origin before any token hook.
        var normalizedUri = requestUri.TrimStart();
        if (normalizedUri.StartsWith("//", StringComparison.Ordinal) ||
            normalizedUri.StartsWith("\\", StringComparison.Ordinal) ||
            !Uri.TryCreate(requestUri, UriKind.Relative, out _) ||
            client.BaseAddress is not { } baseAddress ||
            !Uri.TryCreate(baseAddress, requestUri, out var resolvedUri) ||
            Uri.Compare(baseAddress, resolvedUri, UriComponents.SchemeAndServer,
                UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) != 0)
            throw new ArgumentException("The request URI must be relative to the configured BaseUrl.", nameof(requestUri));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = CreateRequest(method, requestUri, jsonBody, content);
            var outcome = logicalHandler is null
                ? RbiApiLogicalRequestResult.Success(await SendCoreAsync(request, cancellationToken))
                : await logicalHandler.SendAsync(request, SendCoreAsync, cancellationToken);
            if (outcome.Error is { } logicalError)
                return TransportFailure(logicalError, method, stopwatch.Elapsed);
            var response = outcome.Response!;

            if (response.IsSuccessStatusCode) return new(response, null);
            using (response)
                return TransportFailure(
                    await CreateHttpErrorAsync(response, cancellationToken),
                    method,
                    stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            return new(null, CancellationError());
        }
        catch (OperationCanceledException)
        {
            return TransportFailure(TimeoutError(), method, stopwatch.Elapsed);
        }
        catch (BrokenCircuitException)
        {
            return TransportFailure(UnavailableError(), method, stopwatch.Elapsed);
        }
        catch (HttpRequestException exception)
        {
            return TransportFailure(NetworkError(exception.StatusCode), method, stopwatch.Elapsed);
        }
        catch (IOException)
        {
            return TransportFailure(NetworkError(null), method, stopwatch.Elapsed);
        }
    }

    private TransportExecution TransportFailure(RbiApiError error, HttpMethod method, TimeSpan duration)
    {
        LogFailure(error, method, duration);
        return new(null, error);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, object? body, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, requestUri);
        if (content is not null)
            request.Content = content;
        else if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        return request;
    }

    private ValueTask<HttpResponseMessage> SendCoreAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        new(client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken));

    private static async Task<RbiApiError> CreateHttpErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var kind = MapStatus(response.StatusCode);
        var title = DefaultTitle(kind);
        string? detail = null;
        string? bodyTraceId = null;

        var (body, truncated) = await ReadBoundedBodyAsync(response.Content, cancellationToken);
        if (!truncated && body.Length > 0)
        {
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                TryReadJsonError(body, ref title, ref detail, ref bodyTraceId);
            }
            else if (mediaType?.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) == true)
            {
                detail = Limit(Encoding.UTF8.GetString(body).Trim(), 512);
            }
        }

        return new RbiApiError(
            kind,
            response.StatusCode,
            title,
            string.IsNullOrWhiteSpace(detail) ? null : detail,
            bodyTraceId ?? ResolveTraceId(response));
    }

    private static async Task<(byte[] Body, bool Truncated)> ReadBoundedBodyAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        while (buffer.Length <= MaxErrorBodyBytes)
        {
            var remaining = MaxErrorBodyBytes + 1 - (int)buffer.Length;
            var read = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, remaining)), cancellationToken);
            if (read == 0) break;
            buffer.Write(chunk, 0, read);
        }

        return buffer.Length > MaxErrorBodyBytes
            ? ([], true)
            : (buffer.ToArray(), false);
    }

    private static void TryReadJsonError(
        byte[] body,
        ref string title,
        ref string? detail,
        ref string? traceId)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return;

            title = ReadString(document.RootElement, "title")
                ?? ReadString(document.RootElement, "error")
                ?? title;
            detail = ReadString(document.RootElement, "detail")
                ?? ReadString(document.RootElement, "message");
            traceId = ReadString(document.RootElement, "traceId");

            title = Limit(title, 256) ?? DefaultTitle(RbiApiErrorKind.Unknown);
            detail = Limit(detail, 2_000);
            traceId = Limit(traceId, 256);
        }
        catch (JsonException)
        {
            // Unknown or malformed error bodies deliberately fall back to safe status text.
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
    }

    private static string? ResolveTraceId(HttpResponseMessage response)
    {
        foreach (var header in new[] { "X-Correlation-ID", "X-Request-ID" })
        {
            if (response.Headers.TryGetValues(header, out var values))
            {
                var value = Limit(values.FirstOrDefault(), 256);
                if (value is not null) return value;
            }
        }

        return Activity.Current is { } activity ? activity.TraceId.ToString() : null;
    }

    private static RbiApiErrorKind MapStatus(HttpStatusCode statusCode) => (int)statusCode switch
    {
        400 or 422 => RbiApiErrorKind.Validation,
        401 => RbiApiErrorKind.Unauthorized,
        403 => RbiApiErrorKind.Forbidden,
        404 => RbiApiErrorKind.NotFound,
        408 => RbiApiErrorKind.Timeout,
        409 => RbiApiErrorKind.Conflict,
        429 => RbiApiErrorKind.RateLimited,
        >= 500 and <= 599 => RbiApiErrorKind.Server,
        _ => RbiApiErrorKind.Unknown,
    };

    private static string DefaultTitle(RbiApiErrorKind kind) => kind switch
    {
        RbiApiErrorKind.Validation => "The request was not valid.",
        RbiApiErrorKind.Unauthorized => "Authentication is required.",
        RbiApiErrorKind.Forbidden => "The request is not permitted.",
        RbiApiErrorKind.NotFound => "The requested resource was not found.",
        RbiApiErrorKind.Conflict => "The request conflicts with the current resource state.",
        RbiApiErrorKind.RateLimited => "Too many requests were sent.",
        RbiApiErrorKind.Server => "The API could not complete the request.",
        RbiApiErrorKind.Timeout => "The API request timed out.",
        RbiApiErrorKind.Network => "The API could not be reached.",
        RbiApiErrorKind.Cancellation => "The API request was cancelled.",
        RbiApiErrorKind.Unavailable => "The API is temporarily unavailable.",
        _ => "The API request failed.",
    };

    private static RbiApiError CancellationError() =>
        new(RbiApiErrorKind.Cancellation, null, DefaultTitle(RbiApiErrorKind.Cancellation));

    private static RbiApiError TimeoutError() =>
        new(RbiApiErrorKind.Timeout, null, DefaultTitle(RbiApiErrorKind.Timeout));

    private static RbiApiError NetworkError(HttpStatusCode? statusCode) =>
        new(RbiApiErrorKind.Network, statusCode, DefaultTitle(RbiApiErrorKind.Network));

    private static RbiApiError UnavailableError() =>
        new(RbiApiErrorKind.Unavailable, null, DefaultTitle(RbiApiErrorKind.Unavailable),
            "The request was not sent because the API circuit is open.",
            Activity.Current is { } activity ? activity.TraceId.ToString() : null);

    private RbiApiResult<T> Failure<T>(RbiApiError error, HttpMethod method, TimeSpan duration)
    {
        LogFailure(error, method, duration);
        return RbiApiResult<T>.Failure(error);
    }

    private RbiApiResult Failure(RbiApiError error, HttpMethod method, TimeSpan duration)
    {
        LogFailure(error, method, duration);
        return RbiApiResult.Failure(error);
    }

    private void LogFailure(RbiApiError error, HttpMethod method, TimeSpan duration)
    {
        if (error.Kind == RbiApiErrorKind.Cancellation) return;

        var statusCode = error.StatusCode is null ? null : (int?)error.StatusCode.Value;
        // Upstream error/header trace identifiers remain available to callers, but are
        // untrusted response data. Only promote the local Activity identifier into logs.
        var traceId = Activity.Current?.TraceId.ToString();
        if (error.Kind is RbiApiErrorKind.Validation or RbiApiErrorKind.Unauthorized or
            RbiApiErrorKind.Forbidden or RbiApiErrorKind.NotFound or RbiApiErrorKind.Conflict)
        {
            logger.LogDebug(
                "RBI API request completed with a non-success outcome. Client={ClientName} Method={Method} StatusCode={StatusCode} ErrorKind={ErrorKind} DurationMs={DurationMs} TraceId={TraceId}",
                clientName, method.Method, statusCode, error.Kind, duration.TotalMilliseconds, traceId);
            return;
        }

        logger.LogWarning(
            "RBI API request failed. Client={ClientName} Method={Method} StatusCode={StatusCode} ErrorKind={ErrorKind} DurationMs={DurationMs} TraceId={TraceId}",
            clientName, method.Method, statusCode, error.Kind, duration.TotalMilliseconds, traceId);
    }

    private static string? Limit(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= length ? value : value[..length];

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed record TransportExecution(HttpResponseMessage? Response, RbiApiError? Error);
}
