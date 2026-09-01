// File: src/RBBH.ConnectedParties/DL/Entities/Report/Report.cs

using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Report;

/// <summary>
/// Dnevni ili mjesečni izvještaj o stanju izloženosti i limita.
/// </summary>
public class Report
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tip izvještaja: DAILY ili MONTHLY.</summary>
    [Required]
    [StringLength(10)]
    public string ReportType { get; set; } = string.Empty;

    /// <summary>Datum izvještaja (za dnevne) ili prvi dan u mjesecu (za mjesečne).</summary>
    public DateTime ReportDate { get; set; }

    /// <summary>Ukupan broj klijenata u izvještaju.</summary>
    public int TotalClients { get; set; }

    /// <summary>Broj klijenata s prekoračenim limitom.</summary>
    public int ClientsWithBreachedLimit { get; set; }

    /// <summary>Ukupna izloženost prema svim klijentima.</summary>
    public decimal TotalExposure { get; set; }

    /// <summary>Snapshot podataka u JSON formatu (za arhivske potrebe).</summary>
    public string? DataSnapshot { get; set; }

    // ── Audit polja ───────────────────────────────────────────────────────────

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}