using System.Security.Claims;

namespace RBBH.ConnectedParties.Helpers;

public static class AuthenticatedActor
{
    public const string ApplicationUserIdClaim = "rbbh.app_user_id";

    // Keep the raw provider value; callers such as identity binding normalize it
    // explicitly, while self protection historically compares it verbatim.
    public static string? Subject(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static ActorIdentity Capture(ClaimsPrincipal principal) => new(
        Subject(principal), Email(principal),
        Guid.TryParse(principal.FindFirst(ApplicationUserIdClaim)?.Value, out var id) ? id : null);

    public static string Email(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value?.Trim();
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(email))
            throw new MissingActorIdentityException();
        return email;
    }
}

/// <summary>Trusted identity data only; application rules remain in services.</summary>
public sealed record ActorIdentity(string? Subject, string Email, Guid? ApplicationUserId);

public sealed class MissingActorIdentityException() : Exception("Authenticated actor email is required.");
