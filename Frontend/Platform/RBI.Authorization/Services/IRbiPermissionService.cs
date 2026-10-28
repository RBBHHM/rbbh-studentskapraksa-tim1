namespace RBI.Authorization;

/// <summary>Checks permissions for the current Blazor authentication state.</summary>
public interface IRbiPermissionService
{
    /// <summary>Returns whether the current user has the requested permission.</summary>
    ValueTask<bool> HasPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);
}
