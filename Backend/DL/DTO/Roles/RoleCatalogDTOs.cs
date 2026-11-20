using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.Roles;

public sealed record CreateRoleCatalogDTO(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(100)] string DisplayName,
    [StringLength(500)] string? Description,
    [Required, MaxLength(200)] Guid[] PermissionIds);

public sealed record UpdateRoleCatalogDTO(
    [Required, StringLength(100)] string DisplayName,
    [StringLength(500)] string? Description,
    bool IsActive,
    [property: System.Text.Json.Serialization.JsonRequired] DateTime ExpectedVersion,
    [Required, MaxLength(200)] Guid[] PermissionIds);

public sealed record RoleCatalogDTO(Guid Id, string Name, string DisplayName, string? Description,
    bool IsActive, DateTime Version, Guid[] PermissionIds, bool IsProtected);
