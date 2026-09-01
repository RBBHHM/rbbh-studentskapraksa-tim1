using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication;

/// <summary>
/// Lokalni identitet koji omogućava razvoj bez vanjskog Keycloak servisa.
/// Handler se registruje samo kada Keycloak konfiguracija nije postavljena.
/// </summary>
public sealed class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions
{
    public bool Enabled { get; set; }
}

public sealed class DevelopmentAuthenticationHandler : AuthenticationHandler<DevelopmentAuthenticationOptions>
{
    public const string SchemeName = "LocalDevelopment";

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<DevelopmentAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Enabled)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "local-development-user"),
            new("sub", "local-development-user"),
            new(ClaimTypes.Name, "Lokalni razvojni korisnik"),
            new("name", "Lokalni razvojni korisnik"),
            new("preferred_username", "local.admin"),
            new(ClaimTypes.Email, "local.admin@localhost"),
        };

        foreach (var role in new[] { "physical-persons", "legal-persons", "limits", "regulatory-reporting" })
            claims.Add(new Claim(ClaimTypes.Role, role));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
