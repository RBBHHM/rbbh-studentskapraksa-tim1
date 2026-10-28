namespace RBI.Authorization;

/// <summary>Configures application-owned role-to-permission mappings.</summary>
public sealed class RbiAuthorizationOptions
{
    private readonly Dictionary<string, HashSet<string>> _rolePermissions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _authenticatedFallback = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds permissions granted by a role.</summary>
    public RbiAuthorizationOptions MapRole(string role, params string[] permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length is 0)
            throw new ArgumentException("At least one permission is required.", nameof(permissions));

        var normalizedRole = role.Trim();
        if (!_rolePermissions.TryGetValue(normalizedRole, out var mapped))
        {
            mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _rolePermissions.Add(normalizedRole, mapped);
        }

        foreach (var permission in permissions)
            mapped.Add(RbiPermissionName.NormalizeGrant(permission));

        return this;
    }

    /// <summary>Grants explicit permissions to authenticated users with no mapped role.</summary>
    public RbiAuthorizationOptions MapAuthenticatedFallback(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length is 0)
            throw new ArgumentException("At least one permission is required.", nameof(permissions));

        foreach (var permission in permissions)
            _authenticatedFallback.Add(RbiPermissionName.NormalizeGrant(permission));

        return this;
    }

    internal RbiPermissionMap Freeze() => new(_rolePermissions, _authenticatedFallback);
}
