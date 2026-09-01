// File: src/RBBH.ConnectedParties/DL/DTO/LegalEntity/LegalEntityDTOs.cs

using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.LegalEntity;

/// <summary>DTO za kreiranje novog pravnog lica.</summary>
public class CreateLegalEntityDTO
{
    /// <summary>true = rezidentno (zahtijeva TaxNumber), false = nerezidentno (zahtijeva FbaId).</summary>
    [Required]
    public bool IsResident { get; set; }

    /// <summary>Porezni broj — obavezan za rezidente. Format: 13 cifara.</summary>
    public string? TaxNumber { get; set; }

    /// <summary>Matični broj pravnog lica — opcionalan.</summary>
    [StringLength(50)]
    [RegularExpression(@"^\d*$", ErrorMessage = "Matični broj smije sadržavati samo cifre.")]
    public string? MaticniBroj { get; set; }

    /// <summary>FBA ID — obavezan za nerezidente.</summary>
    [RegularExpression(@"^\d{1,10}$", ErrorMessage = "FBA ID mora sadržavati najviše 10 cifara.")]
    public string? FbaId { get; set; }

    [Required(ErrorMessage = "Naziv je obavezan.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Matbroj { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^\d+$", ErrorMessage = "GCC broj mora sadržavati samo cifre.")]
    public string? GccNumber { get; set; }

    [StringLength(100)]
    public string? GccName { get; set; }

    [Required(ErrorMessage = "Osnov povezanosti je obavezan.")]
    [StringLength(100)]
    public string BasisOfConnection { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ConnectionDescription { get; set; }

    public bool? ConnectedWithBank { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>DTO za ažuriranje postojećeg pravnog lica.</summary>
public class UpdateLegalEntityDTO
{
    public bool IsResident { get; set; }

    [StringLength(13)]
    public string? TaxNumber { get; set; }

    [StringLength(10)]
    [RegularExpression(@"^\d{1,10}$", ErrorMessage = "FBA ID mora sadržavati najviše 10 cifara.")]
    public string? FbaId { get; set; }

    [Required(ErrorMessage = "Naziv je obavezan.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? MaticniBroj { get; set; }

    [StringLength(50)]
    public string? Matbroj { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^\d+$", ErrorMessage = "GCC broj mora sadržavati samo cifre.")]
    public string? GccNumber { get; set; }

    [StringLength(200)]
    public string? GccName { get; set; }

    [Required(ErrorMessage = "Osnov povezanosti je obavezan.")]
    [StringLength(100)]
    public string BasisOfConnection { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ConnectionDescription { get; set; }

    public bool? ConnectedWithBank { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }
}

/// <summary>DTO za prikaz pravnog lica u listi i detaljima.</summary>
public class LegalEntityDTO
{
    public Guid Id { get; set; }
    public bool IsResident { get; set; }
    public string? TaxNumber { get; set; }
    public string? MaticniBroj { get; set; }
    public string? FbaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Matbroj { get; set; }
    public string? GccNumber { get; set; }
    public string? GccName { get; set; }
    public string BasisOfConnection { get; set; } = string.Empty;
    public string? ConnectionDescription { get; set; }
    public bool? ConnectedWithBank { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>Response za paginiranu listu pravnih lica.</summary>
public class LegalEntityListDTO
{
    public List<LegalEntityDTO> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}


/// <summary>
/// Skraćeni prikaz pravnog lica za pretragu u formi Limita.
/// </summary>
public class LegalEntityLookupDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MaticniBroj { get; set; }
    public string? TaxNumber { get; set; }
}

/// <summary>
/// Podaci pravnog lica potrebni za automatsko popunjavanje forme Limita.
/// </summary>
public class LegalEntityLimitFormDataDTO
{
    public Guid Id { get; set; }
    public bool IsResident { get; set; }
    public string? FbaId { get; set; }
    public string? TaxNumber { get; set; }
    public string? MaticniBroj { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GccNumber { get; set; }
    public string? GccName { get; set; }
}
