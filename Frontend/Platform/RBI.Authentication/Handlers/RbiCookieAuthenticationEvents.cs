using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace RBI.Authentication.Internal;

internal sealed class RbiCookieAuthenticationEvents(
    IRbiTokenSessionStore sessionStore,
    TimeProvider timeProvider) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sessionId = context.Principal?.FindFirst(RbiAuthenticationSchemes.SessionClaim)?.Value;
        var session = string.IsNullOrWhiteSpace(sessionId)
            ? null
            : await sessionStore.GetAsync(sessionId, context.HttpContext.RequestAborted);

        if (session is null || session.SessionExpiresAt <= timeProvider.GetUtcNow())
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
                await sessionStore.RemoveAsync(sessionId, context.HttpContext.RequestAborted);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(RbiAuthenticationSchemes.Cookie);
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
