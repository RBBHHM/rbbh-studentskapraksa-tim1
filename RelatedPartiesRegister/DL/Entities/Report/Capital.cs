// File: src/RBBH.ConnectedParties/DL/Entities/Report/Capital.cs

using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Report;

/// <summary>
/// Globalni (jedinstveni) podatak o kapitalu banke — modul Kapital.
/// Nije vezan za pravno lice; koristi se kao pomoćna vrijednost u obračunima
/// na drugim mjestima (npr. auto-popunjavanje forme Limita).
/// </summary>
public class Capital
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Regulatorni kapital banke.</summary>
    public decimal RegulatoryCapital { get; set; }

    /// <summary>Osnovni kapital banke.</summary>
    public decimal CoreCapital { get; set; }

    /// <summary>Dopunski kapital banke.</summary>
    public decimal SupplementaryCapital { get; set; }

    /// <summary>Datum na koji se odnose iznosi kapitala.</summary>
    public DateTime CapitalDate { get; set; }

    // ── Audit polja ───────────────────────────────────────────────────────────

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
