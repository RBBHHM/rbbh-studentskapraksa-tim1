using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RBBH.ConnectedParties.DL.Entities.Capital;

public sealed class Capital
{
    public int Id { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OsnovniKapital { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal RegulatorniKapital { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DopunskiKapital { get; set; }
    public DateTime DatumKapitala { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required, StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    [StringLength(100)] public string? ModifiedBy { get; set; }
}
