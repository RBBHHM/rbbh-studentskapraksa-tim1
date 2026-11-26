#nullable enable
using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Role;

public class UserRole
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long RoleId { get; set; }

    [Required]
    public long UserId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    internal void SetActive(bool isActive, string actor, DateTime timestamp)
    {
        if (IsActive == isActive) return;

        IsActive = isActive;
        ModifiedBy = actor;
        ModifiedAt = timestamp;
    }

    public virtual Role Role { get; set; } = null!;
}
