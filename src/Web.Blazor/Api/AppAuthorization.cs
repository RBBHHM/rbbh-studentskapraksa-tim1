using System.Security.Claims;
using RBI.Authorization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

// Consume effective backend grants, never realm roles or a frontend role map.
public sealed class AppPermissionSource(IProfileApi profile) : IRbiPermissionSource
{
    public async ValueTask<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (principal.Identity?.IsAuthenticated != true) return false;
        if (permission == "reports.read") return true; // Approved authenticated read-only exception.
        var effective = await profile.GetAsync(cancellationToken);
        return effective.Permissions.Contains(permission, StringComparer.Ordinal);
    }
}

public static class AppAuthorization
{
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddRbiAuthorization();
        services.AddScoped<IRbiPermissionSource, AppPermissionSource>();
        return services;
    }
}
