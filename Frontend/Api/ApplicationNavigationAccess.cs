using RBI.Authorization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

/// <summary>
/// Shared navigation projection for the dashboard and sidebar. Resolve effective
/// admin status from the DB-owned profile; individual module permissions remain
/// evaluated through the existing authorization service. No cross-request
/// caching: role changes and disabled identities must not retain stale menus.
/// </summary>
public sealed record ApplicationNavigationAccess(
    bool Administrator,
    bool Reports,
    bool Physical,
    bool Legal,
    bool Limits,
    bool Capital)
{
    public static async Task<ApplicationNavigationAccess> ReadAsync(
        IProfileApi profile, IRbiPermissionService permissions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Fail closed before requesting any grants. Never race checks that may
        // use the same circuit-scoped authorization services / DbContext.
        var effectiveProfile = await profile.GetAsync(cancellationToken);
        return new(
            ApplicationAccess.IsAdministrator(effectiveProfile),
            await permissions.HasPermissionAsync("reports.read", cancellationToken),
            await permissions.HasPermissionAsync("fl.read", cancellationToken),
            await permissions.HasPermissionAsync("pl.read", cancellationToken),
            await permissions.HasPermissionAsync("limiti.read", cancellationToken),
            await permissions.HasPermissionAsync("kapital.read", cancellationToken));
    }
}
