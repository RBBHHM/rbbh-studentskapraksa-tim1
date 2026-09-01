// File: src/RBBH.ConnectedParties/DL/Entities/LegalEntity/LegalEntity.cs

using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.LegalEntity;

/// <summary>
/// Pravno lice u registru povezanih lica.
/// Rezidentna pravna lica se identifikuju poreznim brojem (obavezno).
/// Nerezidentna pravna lica se identifikuju FBA ID-om (porezni broj nije aktivan).
/// </summary>
public class LegalEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Tip lica ─────────────────────────────────────────────────────────────

    /// <summary>true = rezidentno, false = nerezidentno</summary>
    public bool IsResident { get; set; }

    // ── Identifikacioni podaci ────────────────────────────────────────────────

    /// <summary>Porezni broj — obavezan za rezidente, null za nerezidente.</summary>
    [StringLength(13)]
    public string? TaxNumber { get; set; }

    /// <summary>Matični broj pravnog lica — opcionalan.</summary>
    [StringLength(50)]
    public string? MaticniBroj { get; set; }
    
    /// <summary>FBA ID — koristi se za nerezidente.</summary>
    [StringLength(10)]
    public string? FbaId { get; set; }

    // ── Osnovni podaci ────────────────────────────────────────────────────────

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    // ── GCC podaci ────────────────────────────────────────────────────────────

    /// <summary>GCC broj.</summary>
    [StringLength(100)]
    public string? GccNumber { get; set; }

    /// <summary>GCC naziv.</summary>
    [StringLength(200)]
    public string? GccName { get; set; }

    // ── Matbroj ───────────────────────────────────────────────────────────────

    /// <summary>Matični broj — neobavezno, numerički string; izvor: kod.dbo.komitent.matbroj.</summary>
    [StringLength(50)]
    public string? Matbroj { get; set; }

    // ── Osnov i opis povezanosti ──────────────────────────────────────────────

    /// <summary>Osnov povezanosti — vrijednost iz šifarnika OSNOV_POVEZANOSTI.</summary>
    [Required]
    [StringLength(100)]
    public string BasisOfConnection { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ConnectionDescription { get; set; }

    // ── Veza s bankom ─────────────────────────────────────────────────────────

    /// <summary>Da li je pravno lice povezano lice sa Bankom (DA/NE).</summary>
    public bool? ConnectedWithBank { get; set; }

    // ── Period važenja ────────────────────────────────────────────────────────

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Status zapisa iz šifarnika — npr. PENDING, VERIFIED, REJECTED, ARCHIVED.</summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft";

    // ── Audit polja (BaseEntity pattern) ─────────────────────────────────────

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    
    [StringLength(100)]
    public string? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; } 

    /// <summary>Soft delete — nikad fizički DELETE.</summary> 
     
    public bool IsActive { get; set; } = true;
}
