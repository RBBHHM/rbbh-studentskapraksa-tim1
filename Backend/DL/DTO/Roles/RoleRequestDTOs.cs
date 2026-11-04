using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.Roles;

public sealed class CreateRoleRequestDTO
{
    public Guid ClientRequestId { get; set; }
    [Required, RegularExpression("^(new|change)$")] public string Purpose { get; set; } = "new";
    [MaxLength(20)] public Guid[] RoleIds { get; set; } = [];
    [MaxLength(1000)] public string? Note { get; set; }
}

public sealed class RoleRequestDecisionDTO
{
    public Guid DecisionId { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}

public sealed record RoleRequestDTO(Guid Id, string Purpose, string Status, Guid[] RoleIds, string[] RoleNames,
    string? Note, DateTime CreatedAt, DateTime? ProcessedAt, string? ProcessedBy, string? DecisionNote,
    int Version, string? RequesterUsername = null, string? RequesterEmail = null);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
