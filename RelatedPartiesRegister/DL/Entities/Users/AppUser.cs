using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Users;

/// <summary>
/// Local user record — Keycloak is the source of truth.
/// This table stores application audit data alongside the Keycloak ID.
/// </summary>
public class AppUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Keycloak user UUID — foreign key to Keycloak.</summary>
    [Required]
    [StringLength(100)]
    public string KeycloakId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
