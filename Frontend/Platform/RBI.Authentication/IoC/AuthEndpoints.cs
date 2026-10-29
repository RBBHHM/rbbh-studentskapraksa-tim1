using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RBI.Authentication.Internal;

namespace RBI.Authentication;

/// <summary>Maps the login and logout endpoints owned by RBI authentication.</summary>
public static class RbiAuthenticationEndpointRouteBuilderExtensions
{
    /// <summary>Maps the RBI authentication endpoints into the application.</summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    /// <returns>The same route builder.</returns>
    public static IEndpointRouteBuilder MapRbiAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/authentication/login", (HttpContext context) =>
        {
            var returnUrl = SafeLocalReturnUrl(context.Request.Query["returnUrl"]);
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                [RbiAuthenticationSchemes.OpenIdConnect]);
        }).AllowAnonymous();

        endpoints.MapPost("/authentication/logout", async (
            HttpContext context,
            IAntiforgery antiforgery,
            IRbiTokenSessionStore sessionStore,
            RbiRefreshCoordinator refreshCoordinator) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var properties = await PrepareLogoutAsync(
                context.User,
                context.Request.Query["returnUrl"],
                sessionStore,
                refreshCoordinator,
                context.RequestAborted);

            return Results.SignOut(
                properties,
                [RbiAuthenticationSchemes.Cookie, RbiAuthenticationSchemes.OpenIdConnect]);
        }).RequireAuthorization();

        return endpoints;
    }

    internal static string SafeLocalReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) &&
        !returnUrl.Any(char.IsControl) &&
        returnUrl[0] == '/' &&
        (returnUrl.Length == 1 || returnUrl[1] is not '/' and not '\\')
            ? returnUrl
            : "/";

    internal static async ValueTask<AuthenticationProperties> PrepareLogoutAsync(
        System.Security.Claims.ClaimsPrincipal user,
        string? returnUrl,
        IRbiTokenSessionStore sessionStore,
        RbiRefreshCoordinator refreshCoordinator,
        CancellationToken cancellationToken)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = SafeLocalReturnUrl(returnUrl),
        };
        var sessionId = user.FindFirst(RbiAuthenticationSchemes.SessionClaim)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId)) return properties;

        var session = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(session?.IdToken))
            properties.Items[RbiOpenIdConnectEvents.IdTokenHintItem] = session.IdToken;
        // Cancel any in-flight refresh in THIS process first so it cannot re-store the session after the removal
        // below. Refreshes running in other instances are not reachable from here.
        refreshCoordinator.Forget(sessionId);
        await sessionStore.RemoveAsync(sessionId, cancellationToken);
        return properties;
    }
}
