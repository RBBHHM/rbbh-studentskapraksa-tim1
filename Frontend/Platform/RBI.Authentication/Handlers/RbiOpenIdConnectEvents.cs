using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace RBI.Authentication.Internal;

internal sealed class RbiOpenIdConnectEvents(
    IRbiTokenSessionStore sessionStore,
    TimeProvider timeProvider,
    ILogger<RbiOpenIdConnectEvents> logger) : OpenIdConnectEvents
{
    internal const string IdTokenHintItem = "rbi:id_token_hint";

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var response = context.TokenEndpointResponse;
        if (response is null ||
            string.IsNullOrWhiteSpace(response.AccessToken) ||
            string.IsNullOrWhiteSpace(response.RefreshToken) ||
            !int.TryParse(response.ExpiresIn, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresIn) ||
            expiresIn <= 0 ||
            context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("The identity provider did not return a usable server-side token session.");
            return;
        }

        var sessionId = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = timeProvider.GetUtcNow();
        var session = new RbiTokenSession(
            response.AccessToken,
            response.RefreshToken,
            response.IdToken,
            now.AddSeconds(expiresIn),
            now + RbiAuthenticationDefaults.SessionLifetime);
        await sessionStore.SetAsync(sessionId, session, context.HttpContext.RequestAborted);
        identity.AddClaim(new Claim(RbiAuthenticationSchemes.SessionClaim, sessionId));
        logger.LogInformation("OIDC sign-in established a protected server-side token session.");
    }

    public override Task RedirectToIdentityProviderForSignOut(
        RedirectContext context)
    {
        if (context.Properties.Items.TryGetValue(IdTokenHintItem, out var idToken) &&
            !string.IsNullOrWhiteSpace(idToken))
            context.ProtocolMessage.IdTokenHint = idToken;

        return Task.CompletedTask;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested)
            return Task.CompletedTask;

        logger.LogError(
            "OIDC authentication failed. FailureType={FailureType}",
            context.Exception.GetType().Name);
        return Task.CompletedTask;
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        logger.LogWarning(
            "OIDC remote authentication failed. FailureType={FailureType}",
            context.Failure?.GetType().Name ?? "unknown");
        context.HandleResponse();
        context.Response.Redirect("/?authentication=failed");
        return Task.CompletedTask;
    }
}
