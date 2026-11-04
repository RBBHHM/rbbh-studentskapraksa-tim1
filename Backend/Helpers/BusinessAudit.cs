using System.Text.Json;
using RBBH.ConnectedParties.DL.Entities.Audit;

namespace RBBH.ConnectedParties.Helpers;

internal static class BusinessAudit
{
    internal static AuditLog Create(string table, string id, string action, string actor, IHttpContextAccessor? accessor,
        object? oldValues = null, object? newValues = null) => new()
    {
        TableName = table, RecordId = id, Action = action,
        UserId = accessor?.HttpContext is { } context ? AuthenticatedActor.RequireSubject(context.User) : actor,
        Username = actor, Timestamp = DateTime.UtcNow,
        IpAddress = accessor?.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
        NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues)
    };
}
