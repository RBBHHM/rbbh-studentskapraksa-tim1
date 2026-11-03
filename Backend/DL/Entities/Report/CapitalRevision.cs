using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Report;

/// <summary>Append-only effective-date observation; corrections create another database-ordered revision.</summary>
public sealed class CapitalRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Revision { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal RegulatoryCapital { get; set; }
    public decimal CoreCapital { get; set; }
    public decimal SupplementaryCapital { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string RecordedBy { get; set; } = string.Empty;
    [StringLength(100)] public string? ActorSubject { get; set; }
}
