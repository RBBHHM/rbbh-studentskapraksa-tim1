using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IAppUserService
{
    Task<Result<GetUsersResponseDTO>> GetUsersAsync(int page, int pageSize, string? search, string? role);
    Task<Result<UserDTO>> CreateUserAsync(CreateUserDTO dto, string createdBy, string? ipAddress = null);
    Task<UserMutationResult> ReplaceRolesAsync(string userId, IReadOnlyCollection<Guid> roleIds, ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken);
    Task<UserMutationResult> DeactivateAsync(string userId, ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken);
    Task<UserMutationResult> ReactivateAsync(string userId, ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken);
    Task<UserMutationResult> DeleteAsync(string userId, ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken);
}
