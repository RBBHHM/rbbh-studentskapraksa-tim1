namespace RBBH.ConnectedParties.DL.DTO;

/// <summary>Rezultat import operacije — broj uvezenih, broj grešaka i poruke grešaka.</summary>
public sealed class ImportResultDTO
{
    public int Imported { get; set; }
    public int Failed   { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<ImportIssue> Details { get; set; } = [];
}

public sealed record ImportIssue(int Row, string Field, string Code, string Message);
