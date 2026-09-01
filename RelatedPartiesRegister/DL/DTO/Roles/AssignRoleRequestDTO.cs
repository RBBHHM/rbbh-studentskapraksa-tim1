namespace RBBH.ConnectedParties.DL.DTO.Roles;

public class AssignRoleRequestDTO
{
    public List<Guid> RoleIds { get; set; } = [];

    /// <summary>Kompatibilnost sa starijim klijentom; novi klijent šalje RoleIds.</summary>
    public Guid RoleId { get; set; }

    public IReadOnlyCollection<Guid> EffectiveRoleIds => RoleIds
        .Concat(RoleId != Guid.Empty ? [RoleId] : [])
        .Where(id => id != Guid.Empty)
        .Distinct()
        .ToArray();
}
