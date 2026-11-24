using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.LegalEntity;

/// <summary>Immutable origin used by the four-eyes verification rule.</summary>
public sealed class LegalEntityVerificationOrigin
{
    [Key]
    public Guid LegalEntityId { get; init; }

    [Required, StringLength(100)]
    public string Subject { get; init; } = string.Empty;
}
