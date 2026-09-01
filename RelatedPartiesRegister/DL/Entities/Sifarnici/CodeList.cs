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

    /// <summary>
    /// Kategorija šifarnika, npr. "TipRola", "OsnovPovezanosti", "VrstaLimita", "TipLica", "TipDokumenta"
    /// </summary>
    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Kategorija { get; set; } = null!;

    /// <summary>
    /// Interni kod vrijednosti (npr. "ADM", "VL", "KR")
    /// </summary>
    [Required]
    [StringLength(50)]
    [Unicode(false)]
    public string Kod { get; set; } = null!;

    /// <summary>
    /// Naziv koji se prikazuje u padajućim menijima
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Naziv { get; set; } = null!;

    /// <summary>
    /// Opcionalni opis vrijednosti
    /// </summary>
    [StringLength(500)]
    public string? Opis { get; set; }

    /// <summary>
    /// Redoslijed prikazivanja u padajućem meniju
    /// </summary>
    public int? RedoslijedPrikaza { get; set; }

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
