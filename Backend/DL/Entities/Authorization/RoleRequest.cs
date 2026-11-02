using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Authorization;

public sealed class RoleRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientRequestId { get; set; }
    [MaxLength(100)] public string RequesterSubject { get; set; } = "";
    [MaxLength(100)] public string Username { get; set; } = "";
    [MaxLength(100)] public string FirstName { get; set; } = "";
    [MaxLength(100)] public string LastName { get; set; } = "";
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(16)] public string Purpose { get; set; } = "new";
    [MaxLength(16)] public string Status { get; set; } = "pending";
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    [MaxLength(100)] public string? ProcessedBy { get; set; }
    [MaxLength(1000)] public string? DecisionNote { get; set; }
    public Guid? DecisionId { get; set; }
    [ConcurrencyCheck] public int Version { get; set; } = 1;
    public List<RequestedRole> RequestedRoles { get; set; } = [];
}

public sealed class RequestedRole
{
    public Guid RequestId { get; set; }
    public RoleRequest Request { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role.Role Role { get; set; } = null!;
}
