namespace RBBH.ConnectedParties.DL.DTO.Limiti;

public class CreateLimitDTO
{
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal? IznosLimita { get; set; }
    public decimal? Utilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

    public decimal? RegulatorniKapital { get; set; }
    public decimal? OsnovniKapital { get; set; }
}

public class UpdateLimitDTO
{
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal? IznosLimita { get; set; }
    public decimal? Utilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

    public decimal? RegulatorniKapital { get; set; }
    public decimal? OsnovniKapital { get; set; }
}

public class LimitResponseDTO
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string TipLimita { get; set; } = string.Empty;

    public decimal IznosLimita { get; set; }
    public decimal Utilizacija { get; set; }
    public decimal? KorigovaniLimit { get; set; }
    public decimal RaspoloziviLimit { get; set; }
    public DateTime? RokUtilizacije { get; set; }
    public string? Komentar { get; set; }

    public decimal RegulatorniKapital { get; set; }
    public decimal OsnovniKapital { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class UpdateCapitalDTO
{
    public decimal? RegulatorniKapital { get; set; }
    public decimal? OsnovniKapital { get; set; }
}
