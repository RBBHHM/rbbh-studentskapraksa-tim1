using System.Text.Json;
using RBBH.ConnectedParties.DL.Entities.Audit;

namespace RBBH.ConnectedParties.Helpers;

internal static class BusinessAudit
{
    internal static AuditLog Create(string table, string id, string action, string actor, IHttpContextAccessor? accessor,
        object? oldValues = null, object? newValues = null)
    {
        if (accessor?.HttpContext is { } context)
            AuthenticatedActor.RequireSubject(context.User);

        return new AuditLog
        {
            TableName = table, RecordId = id, Action = action,
            Username = actor, Timestamp = DateTime.UtcNow,

            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues)
        };
    }
}
