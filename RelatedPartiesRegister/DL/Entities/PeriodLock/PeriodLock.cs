using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.PeriodLock;

/// <summary>
/// Čuva stanje zaključavanja unosa podataka za određeni period (godina + mjesec).
/// Jedan zapis po periodu — kreira se pri prvom zaključavanju.
/// </summary>
public class PeriodLock
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsLocked { get; set; }

    /// <summary>
    /// Odjel za koji važi zaključavanje. Null = globalno (svi odjeli).
    /// Vrijednost odgovara prefiksu korisničkog imena (npr. "compliance", "kadrovski").
    /// </summary>
    [StringLength(50)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }

    [StringLength(100)]
    public string? UnlockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }

    // BaseEntity polja
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
