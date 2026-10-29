using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using RBI.Authorization.Internal;

namespace RBI.Authorization;

internal sealed class RbiPermissionService(
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService) : IRbiPermissionService
{
    public async ValueTask<bool> HasPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default)
    {
        var normalized = RbiPermissionName.Normalize(permission);
        cancellationToken.ThrowIfCancellationRequested();
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var result = await authorizationService.AuthorizeAsync(
            authenticationState.User,
            resource: null,
            [new RbiPermissionRequirement(normalized)]);
        return result.Succeeded;
    }
}
