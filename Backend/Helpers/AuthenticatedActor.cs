using System.Security.Claims;

namespace RBBH.ConnectedParties.Helpers;

public static class AuthenticatedActor
{
    public const string ApplicationUserIdClaim = "rbbh.app_user_id";

    // Provider subjects are opaque. Never trim, case-fold or otherwise normalize them.
    public static string? Subject(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static ActorIdentity Capture(ClaimsPrincipal principal) => new(
        Subject(principal), Email(principal),
        Guid.TryParse(principal.FindFirst(ApplicationUserIdClaim)?.Value, out var id) ? id : null);

    public static string RequireSubject(ClaimsPrincipal principal)
    {
        var subject = Subject(principal);
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(subject) ||
            subject.Length > 100 || subject != subject.Trim()) throw new MissingActorIdentityException();
        return subject;
    }

    public static string Username(ClaimsPrincipal principal)
    {
        var subject = RequireSubject(principal);
        var name = principal.FindFirst("preferred_username")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        return !string.IsNullOrWhiteSpace(name) && name.Length <= 100 ? name : subject;
    }

    public static string Email(ClaimsPrincipal principal)
    {
        var email = (principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value)?.Trim();
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(email))
            throw new MissingActorIdentityException();
        return email;
    }
}

/// <summary>Trusted identity data only; application rules remain in services.</summary>
public sealed record ActorIdentity(string? Subject, string Email, Guid? ApplicationUserId);

public sealed class MissingActorIdentityException() : Exception("Authenticated actor email is required.");
