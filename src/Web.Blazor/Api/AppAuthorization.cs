using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RBI.Authorization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

// Implement through the separate backend authorization API, whose application
// database is authoritative. Never add database access to this frontend.
// Keycloak authenticates identity/email; its role claims are NOT this source.
public interface IApplicationRoleSource
{
    ValueTask<IReadOnlyCollection<string>> GetRolesAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}

internal sealed class UnconfiguredApplicationRoleSource : IApplicationRoleSource
{
    public ValueTask<IReadOnlyCollection<string>> GetRolesAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyCollection<string>>([]);
    }
}

public sealed class AppPermissionSource(IApplicationRoleSource roles) : IRbiPermissionSource
{
    private static readonly HashSet<string> GuestPermissions = ["kapital.read", "pl.read", "fl.read", "limiti.read", "reports.read", "audit.read", "profile.read"];
    private static readonly IReadOnlyDictionary<string, string[]> ModuleGrants = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Kapital"] = ["kapital.create", "kapital.update"],
        ["PL"] = ["pl.create", "pl.update", "pl.delete"],
        ["FL"] = ["fl.create", "fl.update", "fl.delete"],
        ["Limiti"] = ["limiti.create", "limiti.update", "limiti.delete"],
    };

    public async ValueTask<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        permission = permission.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        if (principal.Identity?.IsAuthenticated != true) return false;
        var assigned = await roles.GetRolesAsync(principal, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        // A failed DB lookup must throw/deny, not masquerade as "no role".
        // Regulatory reports remain read-only even for Admin.
        if (permission.StartsWith("reports.", StringComparison.Ordinal) && permission != "reports.read") return false;
        if (assigned.Contains("Admin", StringComparer.Ordinal)) return true;
        if (GuestPermissions.Contains(permission)) return true;
        return assigned.Any(role => ModuleGrants.TryGetValue(role, out var grants) && grants.Contains(permission, StringComparer.Ordinal));
    }
}

public static class AppAuthorization
{
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddRbiAuthorization();
        services.TryAddScoped<IApplicationRoleSource, UnconfiguredApplicationRoleSource>();
        services.AddScoped<IRbiPermissionSource, AppPermissionSource>();
        return services;
    }
}
