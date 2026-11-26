namespace RBBH.ConnectedParties.DL.DTO.Roles;

public class AssignRoleRequestDTO
{
    public List<long> RoleIds { get; set; } = [];

    /// <summary>Kompatibilnost sa starijim klijentom; novi klijent šalje RoleIds.</summary>
    public long RoleId { get; set; }

    public IReadOnlyCollection<long> EffectiveRoleIds => RoleIds
        .Concat(RoleId != 0 ? [RoleId] : [])
        .Where(id => id != 0)
        .Distinct()
        .ToArray();
}
