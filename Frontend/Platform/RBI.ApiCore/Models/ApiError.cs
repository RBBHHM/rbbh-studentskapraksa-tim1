using System.Net;

namespace RBI.ApiCore;

/// <summary>Describes a normalized API transport or HTTP failure.</summary>
/// <param name="Kind">The semantic error category.</param>
/// <param name="StatusCode">The HTTP status code, when a response was received.</param>
/// <param name="Title">A normalized summary that may use a bounded upstream title; application UI should select its own localized wording from Kind.</param>
/// <param name="Detail">Optional bounded upstream or diagnostic detail. It may contain response-body content and must not be assumed safe for direct end-user rendering or logging.</param>
/// <param name="TraceId">The upstream or local correlation identifier, when available.</param>
public sealed record RbiApiError(
    RbiApiErrorKind Kind,
    HttpStatusCode? StatusCode,
    string Title,
    string? Detail = null,
    string? TraceId = null);

/// <summary>Identifies the normalized category of an API failure.</summary>
public enum RbiApiErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    RateLimited,
    Server,
    Timeout,
    Network,
    Cancellation,
    Unavailable,
    Unknown,
}
