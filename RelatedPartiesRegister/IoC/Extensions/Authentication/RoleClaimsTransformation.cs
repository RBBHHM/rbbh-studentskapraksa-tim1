using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public class RoleClaimsTransformation(ConnectedPartiesDbContext db, IConfiguration? configuration = null) : IClaimsTransformation
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
                             claim.Type == "permission" || claim.Type == "rct_done" || claim.Type == "rbbh.app_user_id" ||
                             claim.Type == source.RoleClaimType || claim.Type == ClaimTypes.Role ||
                             claim.Type == "role" || claim.Type == "roles").ToArray())
                    source.RemoveClaim(claim);
            }
            identity ??= new ClaimsIdentity { Label = ApplicationIdentityLabel };

            var resolution = await new ApplicationIdentityResolver(db, configuration).ResolveAsync(principal);
            var user = resolution.User;
            if (resolution.Denied)
            {
                if (addIdentity)
                    principal.AddIdentity(identity);
                return principal;
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, ApplicationAccessRoles.Guest));
            if (user is not null) identity.AddClaim(new Claim("rbbh.app_user_id", user.Id.ToString()));
            var roles = user is null ? new List<string>() : await db.UserRoles.Where(userRole => userRole.IsActive && userRole.UserId == user.Id)
                .Join(db.Roles.Where(role => role.IsActive), userRole => userRole.RoleId, role => role.Id, (userRole, role) => role.Name)
                .Where(role => ApplicationAccessRoles.Assignable.Contains(role))
                .Distinct()
                .ToListAsync();
            foreach (var role in roles)
                identity.AddClaim(new Claim(ClaimTypes.Role, role));

            var permissions = await db.RolePermissions
                .Where(mapping => mapping.Role.IsActive
                    && mapping.Permission.IsActive
                    && roles.Contains(mapping.Role.Name))
                .Select(mapping => mapping.Permission.Code)
                .Distinct()
                .ToListAsync();

            foreach (var permission in permissions.Concat(RolePermissionMatrix.PermissionsByRole[ApplicationAccessRoles.Guest]).Distinct(StringComparer.Ordinal))
                identity.AddClaim(new Claim("permission", permission));

            if (addIdentity)
                principal.AddIdentity(identity);
            return principal;
        }

    }
}
