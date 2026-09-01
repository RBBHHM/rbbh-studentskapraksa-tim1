namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public class AuditEntry
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId  { get; set; } = string.Empty;
    public string Action    { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId    { get; set; } = string.Empty;
    public string Username  { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public interface IAuditService
{
    Task LogAsync(AuditEntry entry);
}
