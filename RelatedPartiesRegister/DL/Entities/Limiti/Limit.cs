using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RBBH.ConnectedParties.DL.Entities.Limiti;

/// <summary>
/// Limit (regulatorni/interni) — npr. limiti izloženosti prema kapitalu banke.
/// Tabela: Limiti.
/// </summary>
public class Limit
{
    [Key]
    public int Id { get; set; }

    public Guid? LegalEntityId { get; set; }
    public virtual RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity? LegalEntity { get; set; }

    /// <summary>Naziv limita — obavezan, maksimalno 100 karaktera.</summary>
    [Required]
    [StringLength(100)]
    public string Naziv { get; set; } = string.Empty;

    /// <summary>Tip/vrsta limita — obavezan (npr. vrijednost iz šifarnika VrstaLimita).</summary>
    [Required]
    [StringLength(100)]
    public string TipLimita { get; set; } = string.Empty;
    
        /// <summary>Odobreni iznos limita.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal IznosLimita { get; set; }

    /// <summary>Maksimalno očekivana utilizacija.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaksimalnoOcekivanaUtilizacija { get; set; }

    /// <summary>Korigovani limit — opcionalan. Ako je popunjen, koristi se za obračun raspoloživog limita.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? KorigovaniLimit { get; set; }

    /// <summary>Rok maksimalno očekivane utilizacije — datum, neobavezno.</summary>
    public DateTime? RokUtilizacije { get; set; }

    /// <summary>Komentar — slobodan tekst, neobavezno.</summary>
    [StringLength(1000)]
    public string? Komentar { get; set; }
    
    // Compatibility only for deserializing historical report snapshots; no longer a Limit DB field.
    [NotMapped]
    public decimal RegulatorniKapital { get; set; }

    [NotMapped]
    public decimal OsnovniKapital { get; set; }

    [NotMapped]
    public decimal DopunskiKapital { get; set; }

    [NotMapped]
    public DateTime? DatumKapitala { get; set; }

    // ─── Audit polja ────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
