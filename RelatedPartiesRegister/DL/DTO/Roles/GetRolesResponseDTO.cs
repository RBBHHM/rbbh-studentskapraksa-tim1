namespace RBBH.ConnectedParties.DL.DTO.Roles;

public class GetRolesResponseDTO
{
    public List<RoleDTO> Roles { get; set; } = new();
}

public class RoleDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
