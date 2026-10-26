using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication;

public sealed record ApplicationIdentity(AppUser? User, bool Denied);

public sealed class ApplicationIdentityResolver(ConnectedPartiesDbContext db, IConfiguration? configuration = null)
{
    public async Task<ApplicationIdentity> ResolveAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return new(null, true);
        var subject = (principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value)?.Trim();
        if (!string.IsNullOrEmpty(subject))
        {
            var confirmed = await db.AppUsers.IgnoreQueryFilters().SingleOrDefaultAsync(user =>
                user.KeycloakSubjectConfirmed && user.KeycloakId == subject);
            if (confirmed is not null) return new(confirmed, !confirmed.IsActive);
            // A historical disabled identifier is a deny marker, never proof of a role grant.
            if (await db.AppUsers.IgnoreQueryFilters().AnyAsync(user => !user.IsActive && user.KeycloakId == subject))
                return new(null, true);
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email)) return new(null, false); // Virtual Guest reads only; mutations require actor email.
        var matches = await db.AppUsers.IgnoreQueryFilters()
            .Where(user => user.Email.Trim().ToLower() == email).Take(2).ToListAsync();
        if (matches.Count == 0) return new(null, false);
        if (matches.Any(user => !user.IsActive)) return new(null, true);
        if (matches.Count != 1 || matches[0].KeycloakSubjectConfirmed)
            return new(null, false); // Virtual Guest only; never link or inherit another subject's explicit roles.

        var verified = principal.FindAll("email_verified").Any(claim => bool.TryParse(claim.Value, out var value) && value);
        if (string.IsNullOrEmpty(subject) || subject.Length > 100 || !verified ||
            configuration?.GetValue<bool>("IdentityBinding:AllowVerifiedEmailFirstLink") != true)
            return new(null, false); // No explicit grants from an unconfirmed legacy/placeholder identifier.

        var candidateId = matches[0].Id;
        return await UsersMutationBoundary.ExecuteAsync(db, async () =>
        {
            var row = await db.AppUsers.IgnoreQueryFilters().SingleAsync(user => user.Id == candidateId);
            if (!row.IsActive) return new ApplicationIdentity(null, true);
            if (row.KeycloakSubjectConfirmed)
                return row.KeycloakId == subject ? new ApplicationIdentity(row, false) : new ApplicationIdentity(null, false);
            // Recheck the compatibility key inside the mutation boundary as well.
            var currentMatches = await db.AppUsers.IgnoreQueryFilters()
                .Where(user => user.Email.Trim().ToLower() == email).Take(2).ToListAsync();
            if (currentMatches.Any(user => !user.IsActive)) return new ApplicationIdentity(null, true);
            if (currentMatches.Count != 1 || currentMatches[0].Id != candidateId)
                return new ApplicationIdentity(null, false);
            var duplicateSubject = await db.AppUsers.IgnoreQueryFilters().AnyAsync(user => user.Id != row.Id && user.KeycloakId == subject);
            if (duplicateSubject) return new ApplicationIdentity(null, true);
            var previousIdentifier = row.KeycloakId;
            row.KeycloakId = subject;
            row.KeycloakSubjectConfirmed = true;
            row.KeycloakSubjectLinkedAt = DateTime.UtcNow;
            row.ModifiedAt = DateTime.UtcNow;
            row.ModifiedBy = principal.FindFirst(ClaimTypes.Email)!.Value;
            await db.SaveChangesAsync();
            await new AuditService(db).LogAsync(new()
            {
                TableName = "AppUser",
                RecordId = row.Id.ToString(),
                Action = "SUBJECT_LINK",
                UserId = subject,
                Username = row.ModifiedBy,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { legacyIdentifier = previousIdentifier }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { confirmedSubject = subject })
            });
            return new ApplicationIdentity(row, false);
        }, () => new ApplicationIdentity(null, true), CancellationToken.None);
    }
}
