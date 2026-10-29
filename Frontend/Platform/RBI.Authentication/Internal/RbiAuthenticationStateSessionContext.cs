using Microsoft.AspNetCore.Components.Authorization;

namespace RBI.Authentication.Internal;

internal sealed class RbiAuthenticationStateSessionContext(
    AuthenticationStateProvider authenticationStateProvider) : IRbiAuthenticationSessionContext
{
    public async ValueTask<string?> GetSessionIdAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated is true
            ? state.User.FindFirst(RbiAuthenticationSchemes.SessionClaim)?.Value
            : null;
    }
}
