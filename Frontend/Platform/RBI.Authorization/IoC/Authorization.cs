using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RBI.Authorization.Internal;

namespace RBI.Authorization;

/// <summary>Registers RBI permission authorization.</summary>
public static class RbiAuthorizationServiceCollectionExtensions
{
    /// <summary>Registers role-backed permissions and standard ASP.NET Core policy integration.</summary>
    public static IServiceCollection AddRbiAuthorization(
        this IServiceCollection services,
        Action<RbiAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        var options = services.AddOptions<RbiAuthorizationOptions>();
        if (configure is not null) options.Configure(configure);
        options.ValidateOnStart();

        services.TryAddSingleton<RbiPermissionMap>();
        services.TryAddScoped<IRbiPermissionSource, RbiRolePermissionSource>();
        services.TryAddScoped<IRbiPermissionService, RbiPermissionService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, RbiPermissionAuthorizationHandler>());
        return services;
    }
}
