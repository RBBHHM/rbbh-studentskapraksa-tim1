using Microsoft.AspNetCore.Builder;
using RBI.Authorization.Internal;

namespace RBI.Authorization;

/// <summary>Adds RBI permission requirements to ASP.NET Core endpoints.</summary>
public static class RbiAuthorizationEndpointConventionBuilderExtensions
{
    /// <summary>Requires an application permission for an endpoint.</summary>
    public static TBuilder RequireRbiPermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        var normalized = RbiPermissionName.Normalize(permission);
        return builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new RbiPermissionRequirement(normalized)));
    }
}
