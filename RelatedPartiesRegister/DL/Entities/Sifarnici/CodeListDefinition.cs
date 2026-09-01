using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Entities.Sifarnici;

public sealed class CodeListDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(100), Unicode(false)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
}
