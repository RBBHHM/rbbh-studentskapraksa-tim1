using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private const string RealmAccessClaim = "realm_access";
        private const string RolesKey = "roles";
        private const string DoneClaim = "rct_done";

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.HasClaim(DoneClaim, "1"))
                return Task.FromResult(principal);

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(DoneClaim, "1"));

            // 1. Keycloak realm_access.roles → ClaimTypes.Role
            var realmAccessClaim = principal.FindFirst(RealmAccessClaim);
            if (realmAccessClaim is not null)
            {
                try
                {
                    var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
                    if (realmAccess.RootElement.TryGetProperty(RolesKey, out var rolesElement))
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrWhiteSpace(roleName) && ApplicationAccessRoles.All.Contains(roleName))
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }
                catch (JsonException) { }
            }

            principal.AddIdentity(identity);
            return Task.FromResult(principal);
        }
    }
}
