using Microsoft.AspNetCore.Authorization;

namespace RBI.Authorization.Internal;

internal sealed class RbiPermissionAuthorizationHandler(IRbiPermissionSource permissionSource)
    : AuthorizationHandler<RbiPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RbiPermissionRequirement requirement)
    {
        if (await permissionSource.HasPermissionAsync(context.User, requirement.Permission))
            context.Succeed(requirement);
    }
}
