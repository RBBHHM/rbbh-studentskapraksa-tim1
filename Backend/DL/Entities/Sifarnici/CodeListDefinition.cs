using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Entities.Sifarnici;

public sealed class CodeListDefinition
{
    public long Id { get; set; }
    [Required, StringLength(100), Unicode(false)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ColumnName15 { get; set; }
    public string? ColumnName17 { get; set; }
    public string? ColumnName18 { get; set; }
    public bool IsSystemRequired { get; set; }
    [ConcurrencyCheck] public DateTime? ModifiedAt { get; set; }
    [StringLength(100)] public string? ModifiedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
}
