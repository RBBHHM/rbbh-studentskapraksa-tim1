using System.Security.Claims;

namespace RBI.Authorization;

internal sealed class RbiRolePermissionSource(RbiPermissionMap map) : IRbiPermissionSource
{
    public ValueTask<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = RbiPermissionName.Normalize(permission);
        if (principal.Identity?.IsAuthenticated is not true)
            return ValueTask.FromResult(false);

        var matchedRole = false;
        foreach (var mapping in map.Roles)
        {
            if (!principal.IsInRole(mapping.Key)) continue;
            matchedRole = true;
            if (mapping.Value.Contains(RbiPermissionName.FullAccess) || mapping.Value.Contains(normalized))
                return ValueTask.FromResult(true);
        }

        var fallbackGranted = !matchedRole &&
            (map.AuthenticatedFallback.Contains(RbiPermissionName.FullAccess) ||
             map.AuthenticatedFallback.Contains(normalized));
        return ValueTask.FromResult(fallbackGranted);
    }
}
