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

    /// <summary>Iskorišteni dio limita.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Utilizacija { get; set; }

    /// <summary>Korigovani limit — opcionalan. Ako je popunjen, koristi se za obračun raspoloživog limita.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? KorigovaniLimit { get; set; }

    /// <summary>Automatski obračunat raspoloživi limit.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal RaspoloziviLimit { get; set; }

    /// <summary>Rok max očekivane utilizacije — datum, neobavezno.</summary>
    public DateTime? RokUtilizacije { get; set; }

    /// <summary>Komentar — slobodan tekst, neobavezno.</summary>
    [StringLength(1000)]
    public string? Komentar { get; set; }
    
    /// <summary>Regulatorni kapital — obavezan broj.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RegulatorniKapital { get; set; }

    /// <summary>Osnovni kapital — obavezan broj.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal OsnovniKapital { get; set; }

    // ─── Audit polja ────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
