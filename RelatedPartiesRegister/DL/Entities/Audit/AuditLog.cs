using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Audit;

/// <summary>
/// Audit log — bilježi sve promjene u sistemu.
/// Nikad se ne soft-deletuje — IsActive je uvijek true.
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required][StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    [Required][StringLength(200)]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>CREATE, UPDATE, DEACTIVATE, REACTIVATE, ROLE_ASSIGN, ROLE_REMOVE,
    /// ADMIN_TRANSFER, CODELIST_ADD, CODELIST_UPDATE, CODELIST_DELETE</summary>
    [Required][StringLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    [Required][StringLength(200)]
    public string UserId { get; set; } = string.Empty;

    [Required][StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
