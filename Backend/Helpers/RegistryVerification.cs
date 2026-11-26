using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace RBBH.ConnectedParties.Helpers;

internal static class RegistryVerification
{
    internal static string? OriginFingerprint(IHttpContextAccessor? accessor)
    {
        // Offline/test construction has no validated actor and cannot establish provenance.
        if (accessor?.HttpContext is not { } context) return null;
        return Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(AuthenticatedActor.RequireSubject(context.User))));
    }

    internal const string CreationProof = "VERIFICATION_ORIGIN";

    internal static void RecordCreator(ConnectedPartiesDbContext db, string table, long id, IHttpContextAccessor? accessor)
    {
        if (OriginFingerprint(accessor) is { } proof)
            db.AuditLogs.Add(new AuditLog
            {
                TableName = table, Action = CreationProof, Username = proof,
                NewValues = RecordKey(id), Timestamp = DateTime.UtcNow
            });
    }

    private static string RecordKey(long id) => id.ToString(CultureInfo.InvariantCulture);

    internal static async Task Authorize(ConnectedPartiesDbContext db, string table, long id, IHttpContextAccessor? accessor)
    {
        var actorFingerprint = OriginFingerprint(accessor) ?? throw new MissingActorIdentityException();
        var key = RecordKey(id);
        var proofs = await db.AuditLogs.IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.TableName == table && row.Action == CreationProof && row.NewValues == key)
            .Select(row => row.Username).Take(2).ToListAsync(accessor?.HttpContext?.RequestAborted ?? default);
        if (proofs.Count != 1 || proofs[0].Length != 64 || !proofs[0].All(Uri.IsHexDigit))
            throw new BusinessRuleException(409, "verification.provenance_required");
        if (string.Equals(proofs[0], actorFingerprint, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException(403, "verification.self_denied");
    }

    private static readonly JsonSerializerOptions AuditFormat = new() { WriteIndented = true };

    internal static AuditLog Audit(string table, string displayActor, IHttpContextAccessor? accessor,
        object before, object after) => new()
    {
        TableName = table, Action = "VERIFY",
        Username = accessor?.HttpContext?.User.FindFirst("preferred_username")?.Value ?? displayActor,

        OldValues = JsonSerializer.Serialize(before, AuditFormat),
        NewValues = JsonSerializer.Serialize(after, AuditFormat),
        Timestamp = DateTime.UtcNow
    };
}
