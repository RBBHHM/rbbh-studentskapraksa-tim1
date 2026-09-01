using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IAppUserService
{
    Task<Result<GetUsersResponseDTO>> GetUsersAsync(int page, int pageSize, string? search, string? role);
    Task<Result<UserDTO>> CreateUserAsync(CreateUserDTO dto, string createdBy);
}
