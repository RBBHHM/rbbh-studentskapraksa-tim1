using System.Collections.Frozen;
using Microsoft.Extensions.Options;

namespace RBI.Authorization;

internal sealed class RbiPermissionMap
{
    public RbiPermissionMap(IOptions<RbiAuthorizationOptions> options)
        : this(options.Value.Freeze())
    {
    }

    internal RbiPermissionMap(RbiPermissionMap snapshot)
    {
        Roles = snapshot.Roles;
        AuthenticatedFallback = snapshot.AuthenticatedFallback;
    }

    internal RbiPermissionMap(
        IReadOnlyDictionary<string, HashSet<string>> rolePermissions,
        IReadOnlySet<string> authenticatedFallback)
    {
        Roles = rolePermissions.ToFrozenDictionary(
            pair => pair.Key,
            pair => pair.Value.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.Ordinal);
        AuthenticatedFallback = authenticatedFallback.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    internal FrozenDictionary<string, FrozenSet<string>> Roles { get; }
    internal FrozenSet<string> AuthenticatedFallback { get; }
}
