namespace RBBH.ConnectedParties.DL.DTO.Capital;

public sealed class SaveCapitalDTO
{
    public decimal? OsnovniKapital { get; set; }
    public decimal? RegulatorniKapital { get; set; }
    public decimal? DopunskiKapital { get; set; }
    public DateTime? DatumKapitala { get; set; }
}

public sealed class CapitalResponseDTO
{
    public int Id { get; set; }
    public decimal OsnovniKapital { get; set; }
    public decimal RegulatorniKapital { get; set; }
    public decimal DopunskiKapital { get; set; }
    public DateTime DatumKapitala { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
