using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public sealed record RoleTransferUser(long UserId, string Username, string Email);
public sealed record RoleTransferTarget(long Id, string Name);
public sealed record RoleDeletionPlan(IReadOnlyList<RoleTransferUser> Users, IReadOnlyList<RoleTransferTarget> Targets);
public sealed record TransferRoleAssignment(long UserId, long TargetRoleId, DateTime ExpectedVersion);

public sealed partial class RoleCatalogService
{
    public async Task<RoleDeletionPlan> DeletionPlan(long id, CancellationToken ct)
    {
        await EnsureCanManage(ct);
        var users = await (from assignment in db.UserRoles
            join user in db.AppUsers on assignment.UserId equals user.Id
            where assignment.RoleId == id && assignment.IsActive
            orderby user.Username, user.Id
            select new RoleTransferUser(user.Id, user.Username, user.Email)).ToListAsync(ct);
        var targets = await db.Roles.Where(role => role.Id != id && role.IsActive && role.Name != ApplicationAccessRoles.Guest)
            .OrderBy(role => role.Name).Select(role => new RoleTransferTarget(role.Id, role.Name)).ToListAsync(ct);
        return new(users, targets);
    }

    public Task<bool> Transfer(long id, TransferRoleAssignment input, CancellationToken ct) => Locked(async () =>
    {
        await EnsureCanManage(ct);
        var source = await db.Roles.SingleOrDefaultAsync(role => role.Id == id, ct);
        if (source is null || Protected(source.Name) || VersionOf(source) != input.ExpectedVersion ||
            await ActorRoleIds().ContainsAsync(id, ct))
            return false;

        var target = await db.Roles.SingleOrDefaultAsync(role => role.Id == input.TargetRoleId &&
            role.Id != id && role.IsActive && role.Name != ApplicationAccessRoles.Guest, ct);
        if (target is null) return false;

        var existing = await db.UserRoles.Where(assignment => assignment.UserId == input.UserId &&
            (assignment.RoleId == id || assignment.RoleId == target.Id)).ToListAsync(ct);
        var from = existing.SingleOrDefault(assignment => assignment.RoleId == id && assignment.IsActive);
        var to = existing.SingleOrDefault(assignment => assignment.RoleId == target.Id);
        if (from is null) return to?.IsActive == true;

        var username = AuthenticatedActor.Username(Actor);
        db.UserRoles.Remove(from);
        if (to is null)
            db.UserRoles.Add(new UserRole { RoleId = target.Id, UserId = input.UserId, CreatedBy = username });
        else
            to.SetActive(true, username, DateTime.UtcNow);
        db.AuditLogs.Add(BusinessAudit.Create("AppUser", "ROLE_ASSIGN", username, accessor,
            new { input.UserId, Role = source.Name }, new { input.UserId, Role = target.Name }));
        await db.SaveChangesAsync(ct);
        return true;
    }, ct);
}
