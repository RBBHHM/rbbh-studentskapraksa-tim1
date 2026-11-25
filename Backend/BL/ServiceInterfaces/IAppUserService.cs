using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IAppUserService
{
    Task<Result<GetUsersResponseDTO>> GetUsersAsync(int page, int pageSize, string? search, string? role);
    Task<Result<UserDTO>> CreateUserAsync(CreateUserDTO dto, string createdBy);
    Task<UserMutationResult> UpdateProfileAsync(string userId, UpdateUserProfileDTO dto, ActorIdentity actor, CancellationToken cancellationToken);
    Task<UserMutationResult> ReplaceRolesAsync(string userId, IReadOnlyCollection<Guid> roleIds, ActorIdentity actor, CancellationToken cancellationToken);
    Task<UserMutationResult> DeactivateAsync(string userId, ActorIdentity actor, CancellationToken cancellationToken);
    Task<UserMutationResult> ReactivateAsync(string userId, ActorIdentity actor, CancellationToken cancellationToken);
    Task<UserMutationResult> DeleteAsync(string userId, ActorIdentity actor, CancellationToken cancellationToken);
}
