namespace RBI.Authentication;

/// <summary>Configures the application-issued confidential OIDC client.</summary>
public sealed class RbiAuthenticationOptions
{
    internal const string SectionName = "Authentication";

    /// <summary>Gets or sets the OIDC authority URL.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Gets or sets the application client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the confidential client secret supplied outside source control.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}
