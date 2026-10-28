using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RBBH.ConnectedParties.DL.Entities.Limiti;

/// <summary>
/// Limit (regulatorni/interni) izloženosti prema konkretnom pravnom licu.
/// Tabela: Limiti.
/// </summary>
public class Limit
{
    [Key]
    public int Id { get; set; }

    /// <summary>Pravno lice na koje se limit odnosi.</summary>
    [Required]
    public Guid LegalEntityId { get; set; }

    /// <summary>Navigacijska veza prema pravnom licu.</summary>
    public LegalEntity.LegalEntity LegalEntity { get; set; } = null!;

    /// <summary>Tip/vrsta limita — obavezan, vrijednost iz šifarnika VrstaLimita.</summary>
    [Required]
    [StringLength(100)]
    public string TipLimita { get; set; } = string.Empty;

    /// <summary>Odobreni iznos limita — neobavezno, dostavlja se ručno (buduća SATA integracija).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OdobreniLimit { get; set; }

    /// <summary>Maksimalna očekivana utilizacija — neobavezno.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }

    /// <summary>Rok očekivane utilizacije — datum, neobavezno.</summary>
    public DateTime? RokOcekivaneUtilizacije { get; set; }

    /// <summary>Komentar — slobodan tekst, neobavezno.</summary>
    [StringLength(1000)]
    public string? Komentar { get; set; }

    // ─── Audit polja ────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
