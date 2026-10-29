namespace RBI.ApiCore;

/// <summary>Configures one named RBI API client.</summary>
public sealed class RbiApiClientOptions
{
    /// <summary>Gets or sets the absolute HTTP or HTTPS base URL.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>Gets or sets the request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the bounded resilience profile.</summary>
    public RbiApiResilienceProfile ResilienceProfile { get; set; }
}

/// <summary>Selects the resilience policy applied to a named API client.</summary>
public enum RbiApiResilienceProfile
{
    /// <summary>Disables retries and circuit breaking.</summary>
    None,

    /// <summary>Uses the standard bounded retry, timeout and circuit-breaker policy.</summary>
    Standard,
}
