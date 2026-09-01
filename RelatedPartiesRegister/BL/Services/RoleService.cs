using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public class RoleService(ConnectedPartiesDbContext dbContext) : IRoleService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    /// <inheritdoc/>
    public async Task<Result<GetRolesResponseDTO>> GetAllRoles()
    {
        var roles = await _dbContext.Roles
            .Where(r => r.IsActive && ApplicationAccessRoles.All.Contains(r.Name))
            .Select(r => new RoleDTO
            {
                Id = r.Id,
                Name = r.Name,
                UserCount = r.UserRoles.Count(ur => ur.IsActive)
            })
            .AsNoTracking()
            .ToListAsync();

        var response = new GetRolesResponseDTO { Roles = roles };
        return Result<GetRolesResponseDTO>.Success(response);
    }
}
