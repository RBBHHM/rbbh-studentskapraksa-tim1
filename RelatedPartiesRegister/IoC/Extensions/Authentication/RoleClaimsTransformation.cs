using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public class RoleClaimsTransformation(ConnectedPartiesDbContext db) : IClaimsTransformation
    {
        private const string DoneClaim = "rct_done";

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.HasClaim(DoneClaim, "1"))
                return principal;

            if (principal.Identity?.IsAuthenticated != true)
                return principal;

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(DoneClaim, "1"));
            identity.AddClaim(new Claim(ClaimTypes.Role, ApplicationAccessRoles.Guest));

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value;
            var roles = new List<string>();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                roles = await db.AppUsers
                    .Where(user => user.IsActive && user.Email.ToLower() == normalizedEmail)
                    .Join(db.UserRoles.Where(userRole => userRole.IsActive), user => user.Id, userRole => userRole.UserId, (user, userRole) => userRole)
                    .Join(db.Roles.Where(role => role.IsActive), userRole => userRole.RoleId, role => role.Id, (userRole, role) => role.Name)
                    .Where(role => ApplicationAccessRoles.All.Contains(role))
                    .Distinct()
                    .ToListAsync();

                foreach (var role in roles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

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

            principal.AddIdentity(identity);
            return principal;
        }
    }
}
