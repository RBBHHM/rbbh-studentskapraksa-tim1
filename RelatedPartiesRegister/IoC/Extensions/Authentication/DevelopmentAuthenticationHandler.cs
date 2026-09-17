using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.Helpers.Constants;

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
    public const string UserHeader = "X-Development-User";

    private static readonly IReadOnlyDictionary<string, (string Id, string Name, string Email)> Users =
        new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin1"] = ("local-admin", "Lokalni administrator", "admin1@raiffeisengroup.ba"),
            ["verifier1"] = ("local-verifier", "Lokalni verifikator", "verifier1@raiffeisengroup.ba")
        };

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

        var requestedUsername = Request.Headers[UserHeader].FirstOrDefault();
        var username = requestedUsername is not null && Users.ContainsKey(requestedUsername)
            ? requestedUsername
            : "admin1";
        var user = Users[username];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("sub", user.Id),
            new(ClaimTypes.Name, user.Name),
            new("name", user.Name),
            new("preferred_username", username),
            new(ClaimTypes.Email, user.Email),
        };

        claims.AddRange(ApplicationAccessRoles.Assignable.Select(role =>
            new Claim(ClaimTypes.Role, role)));
        claims.AddRange(ApplicationPermissions.All.Select(permission =>
            new Claim("permission", permission)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
