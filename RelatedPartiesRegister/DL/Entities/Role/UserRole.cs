#nullable enable
using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Role;

public class UserRole
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = string.Empty;

    public virtual Role Role { get; set; } = null!;
}
