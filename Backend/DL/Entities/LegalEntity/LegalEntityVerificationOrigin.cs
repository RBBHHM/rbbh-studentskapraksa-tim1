using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.LegalEntity;

/// <summary>Immutable origin used by the four-eyes verification rule.</summary>
public sealed class LegalEntityVerificationOrigin
{
    [Key]
    public long LegalEntityId { get; init; }

    [Required, StringLength(64)]
    public string Fingerprint { get; init; } = string.Empty;
}
