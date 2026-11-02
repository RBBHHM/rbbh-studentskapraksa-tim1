using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationAuthentication(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/authentication/login", (string? returnUrl) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturn(returnUrl) }, ["KeycloakOidc"]));

        // Existing links remain compatible until the frontend switches to the CSRF-protected POST.
        endpoints.MapGet("/authentication/logout", (HttpContext context) =>
        {
            context.Response.Headers["Deprecation"] = "true";
            return Logout();
        });
        var authenticated = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        endpoints.MapGet("/authentication/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { requestToken = tokens.RequestToken, headerName = tokens.HeaderName });
        }).RequireAuthorization(authenticated);
        endpoints.MapPost("/authentication/logout", async Task<IResult> (HttpContext context, IAntiforgery antiforgery) =>
        {
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            return Logout();
        }).RequireAuthorization(authenticated);

        endpoints.MapGet("/api/auth/profile", (HttpContext context) => Results.Ok(new
        {
            userId = AuthenticatedActor.Subject(context.User),
            fullName = context.User.FindFirst("name")?.Value ?? context.User.Identity?.Name,
            username = context.User.FindFirst("preferred_username")?.Value,
            email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value,
            applicationUserId = context.User.FindFirst(AuthenticatedActor.ApplicationUserIdClaim)?.Value,
            roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value).Distinct(),
            permissions = context.User.FindAll("permission").Select(claim => claim.Value).Distinct()
        })).RequireAuthorization();
        return endpoints;
    }

    private static IResult Logout() => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, "KeycloakOidc"]);

    public static string SafeReturn(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') &&
        !value.StartsWith("//") && !value.StartsWith("/\\") && !value.Any(char.IsControl) ? value : "/app";
}
