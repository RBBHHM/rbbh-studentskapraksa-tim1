using System.Security.Claims;

namespace RBI.Authorization;

/// <summary>Evaluates one application permission against an authenticated principal.</summary>
public interface IRbiPermissionSource
{
    /// <summary>Returns whether the principal has the requested permission.</summary>
    ValueTask<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken = default);
}
