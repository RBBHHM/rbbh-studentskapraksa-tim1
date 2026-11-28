using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.Roles;

public sealed record CreateRoleCatalogDTO(
    [Required, StringLength(100)] string Name,
    [StringLength(100)] string? DisplayName,
    [StringLength(500)] string? Description,
    [Required, MaxLength(200)] long[] PermissionIds);

public sealed record UpdateRoleCatalogDTO(
    [StringLength(100)] string? DisplayName,
    [StringLength(500)] string? Description,
    bool IsActive,
    [property: System.Text.Json.Serialization.JsonRequired] DateTime ExpectedVersion,
    [Required, MaxLength(200)] long[] PermissionIds);

public sealed record RoleCatalogDTO(long Id, string Name, string DisplayName, string? Description,
    bool IsActive, DateTime Version, long[] PermissionIds, bool IsProtected, bool IsAssignedToCurrentUser = false, bool HasReferences = false);
