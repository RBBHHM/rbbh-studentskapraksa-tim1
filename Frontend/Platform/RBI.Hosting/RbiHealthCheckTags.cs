namespace RBI.Hosting;

/// <summary>
/// Stable tags used to select application-owned health checks.
/// </summary>
public static class RbiHealthCheckTags
{
    /// <summary>
    /// Marks a health check as part of application readiness.
    /// </summary>
    public const string Ready = "ready";
}
