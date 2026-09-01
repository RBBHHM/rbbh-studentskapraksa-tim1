// File: src/RBBH.ConnectedParties/DL/Entities/Report/ClientLimit.cs

using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Report;

/// <summary>
/// Podaci o limitu izloženosti i kapitalu za pravno lice.
/// Jedan zapis po pravnom licu — vezan za LegalEntity.
/// </summary>
public class ClientLimit
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Strani ključ prema LegalEntity.</summary>
    public Guid LegalEntityId { get; set; }

    /// <summary>Navigacijska veza prema pravnom licu.</summary>
    public RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity LegalEntity { get; set; } = null!;

    /// <summary>Regulatorni kapital banke (osnova za izračun limita).</summary>
    public decimal RegulatoryCapital { get; set; }

    /// <summary>Osnovni kapital banke.</summary>
    public decimal CoreCapital { get; set; }

    /// <summary>Iznos limita izloženosti prema klijentu.</summary>
    public decimal ExposureLimit { get; set; }

    /// <summary>Trenutna izloženost banke prema klijentu.</summary>
    public decimal CurrentExposure { get; set; }

    /// <summary>Valuta iznosa (npr. BAM, EUR).</summary>
    [StringLength(3)]
    public string Currency { get; set; } = "BAM";

    /// <summary>Da li je prekoračen limit izloženosti.</summary>
    public bool IsLimitBreached { get; set; }

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