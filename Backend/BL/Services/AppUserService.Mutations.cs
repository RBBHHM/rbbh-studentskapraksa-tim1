using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public partial class AppUserService
{
    public Task<UserMutationResult> UpdateProfileAsync(string userId, DL.DTO.Users.UpdateUserProfileDTO dto,
        ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken) => WithMutationBoundary(async () =>
    {
        var validation = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, new(dto), validation, true))
            throw new Exceptions.BusinessRuleException(400, "users.invalid_profile");
        var row = await ResolveUser(userId, cancellationToken);
        if (row is null) return new(UserMutationStatus.NotFound);
        var username = dto.Username.Trim(); var email = dto.Email.Trim().ToLowerInvariant();
        var normalizedName = username.ToUpperInvariant(); var normalizedEmail = email.ToUpperInvariant();
        if (await _db.AppUsers.AnyAsync(user => user.Id != row.Id &&
            (user.Username.Trim().ToUpper() == normalizedName || user.Email.Trim().ToUpper() == normalizedEmail), cancellationToken))
            throw new Exceptions.BusinessRuleException(409, "users.duplicate_identity");
        var before = JsonSerializer.Serialize(new { row.Username, row.FirstName, row.LastName, row.Email });
        row.Username = username; row.FirstName = dto.FirstName.Trim(); row.LastName = dto.LastName.Trim(); row.Email = email;
        row.ModifiedAt = DateTime.UtcNow; row.ModifiedBy = actor.DisplayName;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditEntry.ForActor("AppUser", row.Id.ToString(), "UPDATE", actor, ipAddress, before,
            JsonSerializer.Serialize(new { row.Username, row.FirstName, row.LastName, row.Email })));
        return new(UserMutationStatus.Success);
    }, cancellationToken);

    public Task<UserMutationResult> ReplaceRolesAsync(string userId, IReadOnlyCollection<Guid> roleIds,
        ActorIdentity actor, string? ipAddress, CancellationToken cancellationToken) =>
        WithMutationBoundary(async () =>
        {
            var localRoles = await _db.Roles
                .Where(role => roleIds.Contains(role.Id) && role.IsActive)
                .ToListAsync(cancellationToken);
            if (localRoles.Count != roleIds.Count || localRoles.Any(role => role.Name == ApplicationAccessRoles.Guest))
                return new(UserMutationStatus.InvalidRoles);

            AppUser? localUser = null;
            if (Guid.TryParse(userId, out var localUserId))
                localUser = await _db.AppUsers.FindAsync([localUserId], cancellationToken);
            localUser ??= await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId, cancellationToken);
            if (localUser is null) return new(UserMutationStatus.NotFound);

            if (!localRoles.Any(role => role.Name == ApplicationAccessRoles.Administrator)
                && await IsFinalActiveAdmin(localUser, cancellationToken))
                return new(UserMutationStatus.FinalAdmin);
            if (IsSelf(localUser, actor)) return new(UserMutationStatus.SelfRoles);

            var isRemoval = roleIds.Count == 0;
            if (isRemoval)
            {
                var assignments = await _db.UserRoles.Where(item => item.UserId == localUser.Id).ToListAsync(cancellationToken);
                _db.UserRoles.RemoveRange(assignments);
            }
            else
                await ReplaceLocalRoles(localUser.Id, roleIds, actor.DisplayName, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var roles = localRoles.Select(role => role.Name).ToList();
            await _audit.LogAsync(AuditEntry.ForActor("AppUser", userId,
                isRemoval ? "ROLE_REMOVE" : "ROLE_ASSIGN", actor, ipAddress,
                newValues: JsonSerializer.Serialize(new { Roles = roles })));
            return new(UserMutationStatus.Success, roles);
        }, cancellationToken);

    public Task<UserMutationResult> DeactivateAsync(string userId, ActorIdentity actor,
        string? ipAddress, CancellationToken cancellationToken) => WithMutationBoundary(async () =>
        {
            var localUser = await ResolveUser(userId, cancellationToken);
            if (localUser is null) return new(UserMutationStatus.NotFound);
            if (IsSelf(localUser, actor)) return new(UserMutationStatus.SelfDeactivate);
            if (await IsFinalActiveAdmin(localUser, cancellationToken)) return new(UserMutationStatus.FinalAdmin);
            localUser.IsActive = false;
            localUser.ModifiedAt = DateTime.UtcNow;
            localUser.ModifiedBy = actor.DisplayName;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(AuditEntry.ForActor("AppUser", userId, "DEACTIVATE", actor, ipAddress,
                oldValues: JsonSerializer.Serialize(new { IsActive = true }),
                newValues: JsonSerializer.Serialize(new { IsActive = false })));
            return new(UserMutationStatus.Success);
        }, cancellationToken);

    public Task<UserMutationResult> ReactivateAsync(string userId, ActorIdentity actor,
        string? ipAddress, CancellationToken cancellationToken) => WithMutationBoundary(async () =>
    {
        AppUser? localUser = Guid.TryParse(userId, out var localId)
            ? await _db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Id == localId, cancellationToken)
            : await _db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.KeycloakId == userId, cancellationToken);
        if (localUser is null) return new(UserMutationStatus.NotFound);
        if (localUser.IsActive) return new(UserMutationStatus.Success);
        localUser.IsActive = true; localUser.ModifiedAt = DateTime.UtcNow; localUser.ModifiedBy = actor.DisplayName;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditEntry.ForActor("AppUser", userId, "REACTIVATE", actor, ipAddress,
            oldValues: JsonSerializer.Serialize(new { IsActive = false }), newValues: JsonSerializer.Serialize(new { IsActive = true })));
        return new(UserMutationStatus.Success);
    }, cancellationToken);

    public Task<UserMutationResult> DeleteAsync(string userId, ActorIdentity actor,
        string? ipAddress, CancellationToken cancellationToken) => WithMutationBoundary(async () =>
        {
            var localUser = await ResolveUser(userId, cancellationToken);
            if (localUser is null) return new(UserMutationStatus.NotFound);
            if (IsSelf(localUser, actor)) return new(UserMutationStatus.SelfDelete);
            if (await IsFinalActiveAdmin(localUser, cancellationToken)) return new(UserMutationStatus.FinalAdmin);

            var assignments = await _db.UserRoles.Where(role => role.UserId == localUser.Id).ToListAsync(cancellationToken);
            _db.UserRoles.RemoveRange(assignments);
            _db.AppUsers.Remove(localUser);
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(AuditEntry.ForActor("AppUser", userId, "DELETE", actor, ipAddress));
            return new(UserMutationStatus.Success);
        }, cancellationToken);

    private Task<UserMutationResult> WithMutationBoundary(Func<Task<UserMutationResult>> mutation, CancellationToken cancellationToken) =>
        UsersMutationBoundary.ExecuteAsync(_db, mutation, () => new(UserMutationStatus.Busy), cancellationToken);

    private async Task<AppUser?> ResolveUser(string id, CancellationToken cancellationToken)
    {
        var local = Guid.TryParse(id, out var localId) ? await _db.AppUsers.FindAsync([localId], cancellationToken) : null;
        return local ?? await _db.AppUsers.SingleOrDefaultAsync(user => user.KeycloakId == id, cancellationToken);
    }

    private static bool IsSelf(AppUser target, ActorIdentity actor) =>
        // Local ID is stripped and recomputed by RoleClaimsTransformation, never request supplied.
        target.Id == actor.ApplicationUserId
        || (target.KeycloakSubjectConfirmed && !string.IsNullOrWhiteSpace(actor.Subject)
            && string.Equals(target.KeycloakId, actor.Subject, StringComparison.Ordinal))
        || string.Equals(target.Email.Trim(), actor.Email, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsFinalActiveAdmin(AppUser target, CancellationToken cancellationToken)
    {
        // Unconfirmed placeholders cannot guarantee continuity of actual Admin
        // access. Two distinct IDs are sufficient to distinguish one from many.
        var activeAdmins = await _db.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed)
            .Join(_db.UserRoles.Where(assignment => assignment.IsActive), user => user.Id, assignment => assignment.UserId,
                (user, assignment) => new { user.Id, assignment.RoleId })
            .Join(_db.Roles.Where(role => role.IsActive && role.Name == ApplicationAccessRoles.Administrator),
                assignment => assignment.RoleId, role => role.Id, (assignment, role) => assignment.Id)
            .Distinct().Take(2).ToListAsync(cancellationToken);
        return activeAdmins.Contains(target.Id) && activeAdmins.Count == 1;
    }

    private async Task ReplaceLocalRoles(Guid userId, IReadOnlyCollection<Guid> roleIds, string actorEmail, CancellationToken cancellationToken)
    {
        var existing = await _db.UserRoles.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        foreach (var item in existing)
            item.IsActive = roleIds.Contains(item.RoleId);

        var existingRoleIds = existing.Select(item => item.RoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
            _db.UserRoles.Add(new DL.Entities.Role.UserRole { UserId = userId, RoleId = roleId, CreatedBy = actorEmail });
    }
}
