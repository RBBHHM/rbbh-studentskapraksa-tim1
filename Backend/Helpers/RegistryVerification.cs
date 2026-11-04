using System.Security.Claims;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.Helpers;

internal static class RegistryVerification
{
    internal static string? CreatorSubject(IHttpContextAccessor? accessor)
    {
        // Offline/test construction has no validated actor and cannot establish provenance.
        if (accessor?.HttpContext is not { } context) return null;
        return AuthenticatedActor.RequireSubject(context.User);
    }

    internal static void Authorize(string? creatorSubject, IHttpContextAccessor? accessor)
    {
        var subject = CreatorSubject(accessor) ?? throw new MissingActorIdentityException();
        if (string.IsNullOrEmpty(creatorSubject))
            throw new BusinessRuleException(409, "verification.provenance_required");
        if (string.Equals(creatorSubject, subject, StringComparison.Ordinal))
            throw new BusinessRuleException(403, "verification.self_denied");
    }

    internal static AuditLog Audit(string table, Guid id, string displayActor, IHttpContextAccessor? accessor) => new()
    {
        TableName = table, RecordId = id.ToString(), Action = "VERIFY",
        UserId = CreatorSubject(accessor)!,
        Username = accessor?.HttpContext?.User.FindFirst("preferred_username")?.Value ?? displayActor,
        IpAddress = accessor?.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        NewValues = "{\"status\":\"Verified\"}", Timestamp = DateTime.UtcNow
    };
}
