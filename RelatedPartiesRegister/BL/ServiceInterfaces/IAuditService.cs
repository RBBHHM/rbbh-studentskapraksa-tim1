namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public class AuditEntry
{
    /// <summary>Common metadata when both audit identifiers are the actor email.</summary>
    public static AuditEntry ForActor(string table, string recordId, string action,
        string actorEmail, string? ipAddress, string? oldValues = null, string? newValues = null) => new()
        {
            TableName = table,
            RecordId = recordId,
            Action = action,
            UserId = actorEmail,
            Username = actorEmail,
            IpAddress = ipAddress,
            OldValues = oldValues,
            NewValues = newValues
        };

    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public interface IAuditService
{
    Task LogAsync(AuditEntry entry);
}
