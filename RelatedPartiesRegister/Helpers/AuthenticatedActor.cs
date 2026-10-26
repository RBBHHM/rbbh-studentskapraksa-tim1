using System.Security.Claims;

namespace RBBH.ConnectedParties.Helpers;

public static class AuthenticatedActor
{
    public static string Email(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value?.Trim();
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(email))
            throw new MissingActorIdentityException();
        return email;
    }
}

public sealed class MissingActorIdentityException() : Exception("Authenticated actor email is required.");
