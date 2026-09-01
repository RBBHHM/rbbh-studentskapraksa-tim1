using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IRoleService
{
    /// <summary>
    /// Returns all active roles with user count.
    /// </summary>
    Task<Result<GetRolesResponseDTO>> GetAllRoles();
}
