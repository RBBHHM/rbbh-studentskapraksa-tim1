using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.Helpers;

internal static class RegistryVerification
{
    internal static string? OriginFingerprint(IHttpContextAccessor? accessor)
    {
        // Offline/test construction has no validated actor and cannot establish provenance.
        if (accessor?.HttpContext is not { } context) return null;
        return Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(AuthenticatedActor.RequireSubject(context.User))));
    }

    internal static void Authorize(string? originFingerprint, IHttpContextAccessor? accessor)
    {
        var actorFingerprint = OriginFingerprint(accessor) ?? throw new MissingActorIdentityException();
        if (string.IsNullOrEmpty(originFingerprint))
            throw new BusinessRuleException(409, "verification.provenance_required");
        if (string.Equals(originFingerprint, actorFingerprint, StringComparison.Ordinal))
            throw new BusinessRuleException(403, "verification.self_denied");
    }

    private static readonly JsonSerializerOptions AuditFormat = new() { WriteIndented = true };

    internal static AuditLog Audit(string table, Guid id, string displayActor, IHttpContextAccessor? accessor,
        object before, object after) => new()
    {
        TableName = table, RecordId = id.ToString(), Action = "VERIFY",
        Username = accessor?.HttpContext?.User.FindFirst("preferred_username")?.Value ?? displayActor,

        OldValues = JsonSerializer.Serialize(before, AuditFormat),
        NewValues = JsonSerializer.Serialize(after, AuditFormat),
        Timestamp = DateTime.UtcNow
    };
}
