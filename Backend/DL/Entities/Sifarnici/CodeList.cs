using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Entities.Sifarnici;

/// <summary>
/// Generički entitet za šifarnike (tipovi rola, osnov povezanosti, vrste limita, itd.)
/// PL-33: Sadrži audit polja — ko je kreirao/izmijenio i kada.
/// </summary>
public class CodeList
{
    [Key]
    public int ID { get; set; }
    public long DefinitionId { get; set; }
    public CodeListDefinition? Definition { get; set; }
    public CodeListLocalization? Localization { get; set; }

    /// <summary>
    /// Kategorija šifarnika, npr. "TIP_ROLA", "OSNOV_POVEZANOSTI", "VRSTE_LIMITA", "TIP_LICA", "TIP_DOKUMENTA"
    /// </summary>
    private string? category;
    [NotMapped]
    public string Kategorija { get => Definition?.Name ?? category ?? ""; set => category = value; }

    /// <summary>
    /// Interni kod vrijednosti (npr. "ADM", "VL", "KR")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Kod { get; set; } = null!;

    /// <summary>
    /// Naziv koji se prikazuje u padajućim menijima
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string Naziv { get; set; } = null!;

    /// <summary>
    /// Opcionalni opis vrijednosti
    /// </summary>
    [StringLength(4000)]
    public string? Opis { get; set; }

    /// <summary>
    /// Da li je vrijednost aktivna (soft-delete flag)
    /// </summary>
    public bool Aktivan { get; set; } = true;

    // ─── Audit polja (PL-33) ───────────────────────────────────────────────

    /// <summary>Datum i vrijeme kreiranja zapisa (UTC)</summary>
    public DateTime KreiranDatum { get; set; }

    /// <summary>Korisnik koji je kreirao zapis</summary>
    [StringLength(100)]
    [Unicode(false)]
    public string KreiraoKorisnik { get; set; } = null!;

    /// <summary>Datum i vrijeme posljednje izmjene (UTC)</summary>
    public DateTime? IzmijenjenDatum { get; set; }

    /// <summary>Korisnik koji je posljednji izmijenio zapis</summary>
    [StringLength(100)]
    [Unicode(false)]
    public string? IzmijenioKorisnik { get; set; }
}

public sealed class CodeListLocalization
{
    [StringLength(10)] public string? LanguageCode { get; set; }
    [StringLength(200)] public string? ResourceKey { get; set; }
    [StringLength(4000)] public string? ResourceLabel { get; set; }
    [StringLength(4000)] public string? ResourceHelper { get; set; }
    [StringLength(4000)] public string? ResourcePlaceholder { get; set; }
    [StringLength(100)] public string? ContextInfo { get; set; }
}
