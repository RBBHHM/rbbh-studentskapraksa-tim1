using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.Users;

public class GetUsersResponseDTO
{
    public List<UserDTO> Users { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserDTO
{
    public Guid Id { get; set; }

    /// <summary>Returns the Keycloak UUID string (same as Id for Keycloak users).</summary>
    public string KeycloakId() => Id.ToString();
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
}

public class CreateUserDTO
{
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
    [EmailAddress]
    [RegularExpression(@"(?i)^[^@\s]+@raiffeisengroup\.ba$",
        ErrorMessage = "Email adresa mora završavati domenom @raiffeisengroup.ba.")]
    public string Email { get; set; } = string.Empty;

    public List<Guid> RoleIds { get; set; } = [];

    /// <summary>Kompatibilnost sa starijim klijentom.</summary>
    public string? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
}
