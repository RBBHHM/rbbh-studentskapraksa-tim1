using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.PeriodLock;

/// <summary>
/// Zahtjev korisnika za otključavanjem perioda.
/// Status: PENDING → APPROVED ili REJECTED.
/// Nikad se fizički ne briše (soft delete).
/// </summary>
public class UnlockRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string RequestedBy { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RequestedByEmail { get; set; } = string.Empty;

    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Min 10, max 500 karaktera.</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>PENDING | APPROVED | REJECTED</summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "PENDING";

    [StringLength(100)]
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Napomena admina — popunjava se pri odbijanju ili zahtjevu za više informacija.
    /// </summary>
    [StringLength(1000)]
    public string? AdminNote { get; set; }

    // BaseEntity polja
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
