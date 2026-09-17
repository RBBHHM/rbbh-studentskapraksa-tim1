namespace RBBH.ConnectedParties.DL.DTO.Limiti;

public class CreateLimitDTO
{
    public Guid? LegalEntityId { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal? IznosLimita { get; set; }
    public decimal? MaksimalnoOcekivanaUtilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

}

public class UpdateLimitDTO
{
    public Guid? LegalEntityId { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal? IznosLimita { get; set; }
    public decimal? MaksimalnoOcekivanaUtilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

}

public class LimitResponseDTO
{
    public int Id { get; set; }
    public Guid? LegalEntityId { get; set; }
    public bool? IsResident { get; set; }
    public string LegalEntityName { get; set; } = string.Empty;
    public string? FbaId { get; set; }
    public string? TaxNumber { get; set; }
    public string? MaticniBroj { get; set; }
    public string? GCCNumber { get; set; }
    public string? GCCName { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal IznosLimita { get; set; }
    public decimal MaksimalnoOcekivanaUtilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

    public decimal? RegulatorniKapital { get; set; }
    public decimal? OsnovniKapital { get; set; }
    public DateTime? DatumKapitala { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
