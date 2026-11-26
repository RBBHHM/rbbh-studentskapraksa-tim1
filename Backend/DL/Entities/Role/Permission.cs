#nullable enable
using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Role;

public class Permission
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Module { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}