using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public class RoleClaimsTransformation(ConnectedPartiesDbContext db) : IClaimsTransformation
    {
        private const string ApplicationIdentityLabel = "RBBH.ApplicationAuthorization";

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true)
                return principal;

            // Identity-provider claims authenticate the user, never grant application
            // permissions. Recompute from the DB, including repeated transforms, so
            // an incoming/serialized marker cannot bypass lookup or retain old grants.
            var identity = principal.Identities.FirstOrDefault(item => item.Label == ApplicationIdentityLabel);
            var addIdentity = identity is null;
            foreach (var source in principal.Identities)
            {
                foreach (var claim in source.Claims.Where(claim =>
                             claim.Type == "permission" || claim.Type == "rct_done" ||
                             claim.Type == source.RoleClaimType || claim.Type == ClaimTypes.Role ||
                             claim.Type == "role" || claim.Type == "roles").ToArray())
                    source.RemoveClaim(claim);
            }
            identity ??= new ClaimsIdentity { Label = ApplicationIdentityLabel };

            // Only a known, active AppUser receives application access (Guest included).
            // An authenticated identity without an AppUser gets no business grants.
            var user = await ResolveApplicationUserAsync(principal);
            if (user is null)
            {
                if (addIdentity)
                    principal.AddIdentity(identity);
                return principal;
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, ApplicationAccessRoles.Guest));
            var roles = await db.UserRoles.Where(userRole => userRole.IsActive && userRole.UserId == user.Id)
                .Join(db.Roles.Where(role => role.IsActive), userRole => userRole.RoleId, role => role.Id, (userRole, role) => role.Name)
                .Where(role => ApplicationAccessRoles.All.Contains(role))
                .Distinct()
                .ToListAsync();
            foreach (var role in roles)
                identity.AddClaim(new Claim(ClaimTypes.Role, role));

            var effectiveRoleNames = roles
                .Append(ApplicationAccessRoles.Guest)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var permissions = await db.RolePermissions
                .Where(mapping => mapping.Role.IsActive
                    && mapping.Permission.IsActive
                    && effectiveRoleNames.Contains(mapping.Role.Name))
                .Select(mapping => mapping.Permission.Code)
                .Distinct()
                .ToListAsync();

            foreach (var permission in permissions)
                identity.AddClaim(new Claim("permission", permission));

            if (addIdentity)
                principal.AddIdentity(identity);
            return principal;
        }

        // Stable binding is the Keycloak subject. Email is only a bootstrap fallback
        // for accounts not yet bound to a subject, and only when the provider says it
        // is verified and exactly one active account matches the normalized address.
        private async Task<DL.Entities.Users.AppUser?> ResolveApplicationUserAsync(ClaimsPrincipal principal)
        {
            var subject = (principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value)?.Trim();
            if (!string.IsNullOrEmpty(subject))
            {
                var bound = await db.AppUsers.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(user => user.KeycloakId == subject);
                // A subject that belongs to an account is final: no email fallback,
                // and an inactive account yields no access.
                if (bound is not null)
                    return bound.IsActive ? bound : null;
            }

            var email = (principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value)?.Trim();
            if (string.IsNullOrEmpty(email) || !IsEmailVerified(principal))
                return null;

            var normalizedEmail = email.ToLower();
            var matches = await db.AppUsers
                .Where(user => user.IsActive && user.Email.ToLower() == normalizedEmail)
                .Take(2)
                .ToListAsync();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool IsEmailVerified(ClaimsPrincipal principal) =>
            principal.FindAll("email_verified").Any(claim => bool.TryParse(claim.Value, out var verified) && verified);
    }
}
