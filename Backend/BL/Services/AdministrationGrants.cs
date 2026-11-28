using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

internal static class AdministrationGrants
{
    // Recheck current grants inside the same transaction that applies administrative changes.
    internal static IQueryable<AppUser> Managers(ConnectedPartiesDbContext db)
    {
        var grants = from assignment in db.UserRoles
            where assignment.IsActive && assignment.Role.IsActive
            from grant in assignment.Role.RolePermissions
            where grant.Permission.IsActive
            select new { assignment.UserId, grant.Permission.Code };

        return db.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed &&
            grants.Any(grant => grant.UserId == user.Id && grant.Code == ApplicationPermissions.AdministrationView) &&
            grants.Any(grant => grant.UserId == user.Id && grant.Code == ApplicationPermissions.AdministrationManage));
    }
}
