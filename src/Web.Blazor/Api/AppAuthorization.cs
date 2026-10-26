using System.Security.Claims;
using RBI.Authorization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

// Consume effective backend grants, never realm roles or a frontend role map.
public sealed class AppPermissionSource(IProfileApi profile) : IRbiPermissionSource
{
    // RBI permission names are semantic dot notation; the backend's established
    // uppercase/underscore wire codes are deliberately unchanged. This is a
    // permission-name adapter, not a role→grant mapping.
    private static readonly IReadOnlyDictionary<string, string> BackendCodes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["fl.read"] = "FL_VIEW",
        ["fl.create"] = "FL_CREATE",
        ["fl.update"] = "FL_EDIT",
        ["fl.delete"] = "FL_DELETE",
        ["pl.read"] = "PL_VIEW",
        ["pl.create"] = "PL_CREATE",
        ["pl.update"] = "PL_EDIT",
        ["pl.delete"] = "PL_DELETE",
        ["limiti.read"] = "LIMITI_VIEW",
        ["limiti.create"] = "LIMITI_CREATE",
        ["limiti.update"] = "LIMITI_EDIT",
        ["limiti.delete"] = "LIMITI_DELETE",
        ["kapital.read"] = "KAPITAL_VIEW",
        ["kapital.update"] = "KAPITAL_EDIT",
        ["reports.generate"] = "KAPITAL_CREATE",
        ["administration.read"] = "ADMINISTRATION_VIEW",
        ["administration.manage"] = "ADMINISTRATION_MANAGE",
        ["codelists.read"] = "CODE_LISTS_VIEW"
    };
    public async ValueTask<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (principal.Identity?.IsAuthenticated != true) return false;
        if (permission == "reports.read") return ApplicationAccess.IsKnownApplicationUser(await profile.GetAsync(cancellationToken));
        if (!BackendCodes.TryGetValue(permission, out var backendCode)) return false;
        var effective = await profile.GetAsync(cancellationToken);
        return effective.Permissions.Contains(backendCode, StringComparer.Ordinal);
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
