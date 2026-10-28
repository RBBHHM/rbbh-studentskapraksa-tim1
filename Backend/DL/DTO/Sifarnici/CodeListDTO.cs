namespace RBBH.ConnectedParties.DL.DTO.Sifarnici;

// ─── Response DTO ─────────────────────────────────────────────────────────────

/// <summary>
/// Vraća se pri čitanju šifarnika — uključuje audit podatke.
/// </summary>
public class CodeListResponseDTO
{
    public int ID { get; set; }
    public string Kategorija { get; set; } = null!;
    public string Kod { get; set; } = null!;
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }
    public int? RedoslijedPrikaza { get; set; }
    public bool Aktivan { get; set; }

    // Audit polja
    public DateTime KreiranDatum { get; set; }
    public string KreiraoKorisnik { get; set; } = null!;
    public DateTime? IzmijenjenDatum { get; set; }
    public string? IzmijenioKorisnik { get; set; }
}

// ─── Create DTO ───────────────────────────────────────────────────────────────

/// <summary>
/// Tijelo zahtjeva za kreiranje nove vrijednosti šifarnika.
/// </summary>
public class CreateCodeListDTO
{
    public string Kategorija { get; set; } = null!;
    public string Kod { get; set; } = null!;
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }
    public int? RedoslijedPrikaza { get; set; }
    public bool Aktivan { get; set; } = true;
}

// ─── Update DTO ───────────────────────────────────────────────────────────────

/// <summary>
/// Tijelo zahtjeva za ažuriranje postojeće vrijednosti šifarnika.
/// Kod i Kategorija se ne mijenjaju — samo Naziv, Opis, Redoslijed.
/// </summary>
public class UpdateCodeListDTO
{
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }
    public int? RedoslijedPrikaza { get; set; }
    public bool Aktivan { get; set; } = true;
}

// ─── Simple value request (API contract) ──────────────────────────────────────

/// <summary>POST /api/code-lists/{category} body: { "value": "..." }</summary>
public class SimpleValueRequest
{
    public string Value { get; set; } = string.Empty;
}

// ─── Import DTO ───────────────────────────────────────────────────────────────

/// <summary>
/// Rezultat pregleda/uvoza Excel fajla.
/// Kada je dryRun=true, Imported ostaje 0 — samo preview podataka.
/// </summary>
public class ImportPreviewDto
{
    public int TotalRows { get; set; }
    public List<string> ToImport  { get; set; } = [];
    public List<string> Duplicates { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public int Imported { get; set; }
}

// ─── Dropdown DTO ─────────────────────────────────────────────────────────────

/// <summary>
/// Minimalni DTO za padajuće menije — samo Kod i Naziv.
/// </summary>
public class CodeListDropdownDTO
{
    public string Kod { get; set; } = null!;
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }
}

public sealed class CreateCodeListDefinitionDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
