namespace RBBH.ConnectedParties.DL.DTO.Limiti;

public class CreateLimitDTO
{
    public Guid LegalEntityId { get; set; }
    public string TipLimita { get; set; } = string.Empty;

    public decimal? OdobreniLimit { get; set; }
    public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }
    public DateTime? RokOcekivaneUtilizacije { get; set; }
    public string? Komentar { get; set; }
}

public class UpdateLimitDTO
{
    public Guid LegalEntityId { get; set; }
    public string TipLimita { get; set; } = string.Empty;

    public decimal? OdobreniLimit { get; set; }
    public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }
    public DateTime? RokOcekivaneUtilizacije { get; set; }
    public string? Komentar { get; set; }
}

public class LimitResponseDTO
{
    public int Id { get; set; }
    public Guid LegalEntityId { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public bool IsResident { get; set; }
    public string? FbaId { get; set; }
    public string? TaxNumber { get; set; }
    public string? MaticniBroj { get; set; }
    public string? Matbroj { get; set; }
    public string TipLimita { get; set; } = string.Empty;

    public decimal? OdobreniLimit { get; set; }
    public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }
    public DateTime? RokOcekivaneUtilizacije { get; set; }
    public string? Komentar { get; set; }

    public decimal? RegulatorniKapital { get; set; }
    public decimal? OsnovniKapital { get; set; }
    public DateTime? DatumKapitala { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
